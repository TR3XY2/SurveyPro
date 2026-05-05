// <copyright file="SurveyParticipationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SurveyPro.Application.Common;
using SurveyPro.Application.DTOs.Participation;
using SurveyPro.Application.Interfaces;
using SurveyPro.Domain.Entities;
using SurveyPro.Domain.Enums;
using SurveyPro.Infrastructure.Persistence;

/// <summary>
/// Participation service implementation that uses DbContext for responses.
/// </summary>
public sealed class SurveyParticipationService : ISurveyParticipationService
{
    private const string SurveyNotPublishedMessage = "This survey is being configured and is not available yet.";

    private readonly SurveyProDbContext dbContext;
    private readonly ILogger<SurveyParticipationService> logger;

    public SurveyParticipationService(
        SurveyProDbContext dbContext,
        ILogger<SurveyParticipationService> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<Result<SurveyParticipationDto>> GetByCodeAsync(
        string code,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result<SurveyParticipationDto>.Failure("Access code is required.");
        }

        var session = await this.dbContext.SurveySessions
            .AsNoTracking()
            .Include(s => s.Survey)
                .ThenInclude(s => s.Questions)
                    .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(s => s.AccessCode == code.Trim() && s.IsActive, cancellationToken);

        if (session == null)
        {
            return Result<SurveyParticipationDto>.Failure("Survey not found.");
        }

        var survey = session.Survey;
        if (survey.Status != SurveyStatuses.Published)
        {
            return Result<SurveyParticipationDto>.Failure(SurveyNotPublishedMessage);
        }

        var dto = new SurveyParticipationDto
        {
            SurveyId = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            AccessCode = session.AccessCode,
            IsPublic = survey.IsPublic,
            Questions = survey.Questions
                .OrderBy(q => q.OrderNumber)
                .Select(q => new ParticipationQuestionDto
                {
                    QuestionId = q.Id,
                    Text = q.Text,
                    Type = q.Type,
                    OrderNumber = q.OrderNumber,
                    Options = q.Options
                        .OrderBy(o => o.Text)
                        .Select(o => new ParticipationOptionDto
                        {
                            Id = o.Id,
                            Text = o.Text,
                        })
                        .ToList(),
                })
                .ToList(),
        };

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            dto.DraftAnswers = await this.LoadDraftAnswersAsync(session.Id, userId.Value, cancellationToken);
            dto.IsSubmitted = await this.IsAlreadySubmittedAsync(session.Id, userId.Value, cancellationToken);
        }

        return Result<SurveyParticipationDto>.Success(dto);
    }

    public async Task<Result> SaveDraftAsync(
        Guid userId,
        SaveDraftRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationResult = ValidateSaveDraftRequest(userId, request);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        var session = await this.GetActiveSessionByCodeAsync(request.AccessCode, cancellationToken);
        if (session == null)
        {
            return Result.Failure("Survey not found.");
        }

        if (session.Survey.Status != SurveyStatuses.Published)
        {
            return Result.Failure(SurveyNotPublishedMessage);
        }

        if (session.SurveyId != request.SurveyId)
        {
            return Result.Failure("Survey mismatch.");
        }

        var participant = await this.GetOrCreateParticipantAsync(session.Id, userId, cancellationToken);
        var draftResponse = await this.GetOrCreateDraftResponseAsync(participant.Id, cancellationToken);

        await this.ReplaceDraftAnswersAsync(draftResponse.Id, request.Answers, session.Survey.Questions, cancellationToken);

        await this.dbContext.SaveChangesAsync(cancellationToken);

        var alreadySubmitted = await this.IsAlreadySubmittedAsync(session.Id, userId, cancellationToken);
        if (alreadySubmitted)
        {
            return Result.Failure("Survey already submitted. You cannot edit your answers.");
        }

        this.logger.LogInformation(
            "Draft saved for survey {SurveyId} by user {UserId}",
            session.SurveyId,
            userId);

        return Result.Success();
    }

    public async Task<Result> ClearDraftAsync(Guid userId, string accessCode, CancellationToken ct)
    {
        var session = await this.GetActiveSessionByCodeAsync(accessCode, ct);

        if (session == null)
        {
            return Result.Failure("Survey not found.");
        }

        if (session.Survey.Status != SurveyStatuses.Published)
        {
            return Result.Failure(SurveyNotPublishedMessage);
        }

        var participant = await this.dbContext.SessionParticipants
            .FirstOrDefaultAsync(p => p.SessionId == session.Id && p.UserId == userId, ct);

        if (participant == null)
        {
            return Result.Success();
        }

        var draft = await this.dbContext.Responses
            .Include(r => r.Answers)
            .FirstOrDefaultAsync(
                r => r.SessionParticipantId == participant.Id && r.IsDraft,
                ct);

        if (draft == null)
        {
            return Result.Success();
        }

        this.dbContext.ResponseAnswers.RemoveRange(draft.Answers);

        await this.dbContext.SaveChangesAsync(ct);

        var alreadySubmitted = await this.IsAlreadySubmittedAsync(session.Id, userId, ct);
        if (alreadySubmitted)
        {
            return Result.Failure("Survey already submitted.");
        }

        return Result.Success();
    }

    public async Task<Result> SubmitAsync(Guid userId, string accessCode, Guid surveyId, CancellationToken ct)
    {
        var session = await this.GetActiveSessionByCodeAsync(accessCode, ct);

        if (session == null)
        {
            return Result.Failure("Survey not found.");
        }

        if (session.Survey.Status != SurveyStatuses.Published)
        {
            return Result.Failure(SurveyNotPublishedMessage);
        }

        if (session.SurveyId != surveyId)
        {
            return Result.Failure("Survey mismatch.");
        }

        var participant = await this.dbContext.SessionParticipants
            .FirstOrDefaultAsync(p => p.SessionId == session.Id && p.UserId == userId, ct);

        if (participant == null)
        {
            return Result.Failure("No draft found.");
        }

        var draft = await this.dbContext.Responses
            .Include(r => r.Answers)
            .FirstOrDefaultAsync(r => r.SessionParticipantId == participant.Id && r.IsDraft, ct);

        if (draft == null)
        {
            return Result.Failure("No draft to submit.");
        }

        var answeredQuestionIds = draft.Answers
            .Select(a => a.QuestionId)
            .Distinct()
            .ToHashSet();

        var allQuestionIds = session.Survey.Questions
        .Select(q => q.Id)
        .ToHashSet();

        if (!allQuestionIds.IsSubsetOf(answeredQuestionIds) || answeredQuestionIds.Count < allQuestionIds.Count)
        {
            var unansweredCount = allQuestionIds.Except(answeredQuestionIds).Count();
            return Result.Failure($"Please answer all questions. {unansweredCount} question(s) remaining.");
        }

        draft.IsDraft = false;
        draft.SubmittedAt = DateTime.UtcNow;

        await this.dbContext.SaveChangesAsync(ct);

        await this.QueueSurveyResponseSubmittedNotificationAsync(session.Survey, userId, ct);

        this.logger.LogInformation(
            "Survey {SurveyId} submitted by user {UserId}",
            surveyId,
            userId);

        return Result.Success();
    }

    private static Result ValidateSaveDraftRequest(Guid userId, SaveDraftRequestDto request)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure("Invalid user id.");
        }

        if (string.IsNullOrWhiteSpace(request.AccessCode))
        {
            return Result.Failure("Access code is required.");
        }

        return Result.Success();
    }

    private async Task<SurveySession?> GetActiveSessionByCodeAsync(string accessCode, CancellationToken cancellationToken)
    {
        return await this.dbContext.SurveySessions
            .Include(s => s.Survey)
                .ThenInclude(s => s.Questions)
                    .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(
                s => s.AccessCode == accessCode.Trim() && s.IsActive,
                cancellationToken);
    }

    private async Task<SessionParticipant> GetOrCreateParticipantAsync(
        Guid sessionId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var participant = await this.dbContext.SessionParticipants
            .FirstOrDefaultAsync(
                sp => sp.SessionId == sessionId && sp.UserId == userId,
                cancellationToken);

        if (participant != null)
        {
            return participant;
        }

        participant = new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
        };

        await this.dbContext.SessionParticipants.AddAsync(participant, cancellationToken);
        return participant;
    }

    private async Task<Response> GetOrCreateDraftResponseAsync(Guid participantId, CancellationToken cancellationToken)
    {
        var draftResponse = await this.dbContext.Responses
            .Include(r => r.Answers)
            .FirstOrDefaultAsync(
                r => r.SessionParticipantId == participantId && r.IsDraft,
                cancellationToken);

        if (draftResponse != null)
        {
            return draftResponse;
        }

        draftResponse = new Response
        {
            Id = Guid.NewGuid(),
            SessionParticipantId = participantId,
            CreatedAt = DateTime.UtcNow,
            IsDraft = true,
        };

        await this.dbContext.Responses.AddAsync(draftResponse, cancellationToken);
        return draftResponse;
    }

    private async Task ReplaceDraftAnswersAsync(
        Guid draftResponseId,
        IReadOnlyCollection<ParticipationAnswerDto> draftAnswers,
        ICollection<Question> questions,
        CancellationToken cancellationToken)
    {
        var existingAnswers = await this.dbContext.ResponseAnswers
            .Where(answer => answer.ResponseId == draftResponseId)
            .ToListAsync(cancellationToken);

        this.dbContext.ResponseAnswers.RemoveRange(existingAnswers);

        var answerRows = this.BuildDraftAnswerRows(draftResponseId, draftAnswers, questions);
        if (answerRows.Count == 0)
        {
            return;
        }

        await this.dbContext.ResponseAnswers.AddRangeAsync(answerRows, cancellationToken);
    }

    private List<ResponseAnswer> BuildDraftAnswerRows(
        Guid responseId,
        IReadOnlyCollection<ParticipationAnswerDto> draftAnswers,
        ICollection<Question> questions)
    {
        var answersByQuestionId = draftAnswers.ToDictionary(answer => answer.QuestionId);
        var answerRows = new List<ResponseAnswer>();

        foreach (var question in questions)
        {
            if (!answersByQuestionId.TryGetValue(question.Id, out var answer))
            {
                continue;
            }

            answerRows.AddRange(this.MapAnswerRows(question, answer, responseId));
        }

        return answerRows;
    }

    private IReadOnlyCollection<ResponseAnswer> MapAnswerRows(
        Question question,
        ParticipationAnswerDto answer,
        Guid responseId)
    {
        if (question.Type == "Text")
        {
            if (string.IsNullOrWhiteSpace(answer.TextAnswer))
            {
                return Array.Empty<ResponseAnswer>();
            }

            return new[]
            {
                new ResponseAnswer
                {
                    Id = Guid.NewGuid(),
                    ResponseId = responseId,
                    QuestionId = question.Id,
                    TextAnswer = answer.TextAnswer.Trim(),
                },
            };
        }

        if (question.Type == "MultipleChoice")
        {
            return answer.SelectedOptionIds
                .Distinct()
                .Select(optionId => new ResponseAnswer
                {
                    Id = Guid.NewGuid(),
                    ResponseId = responseId,
                    QuestionId = question.Id,
                    OptionId = optionId,
                })
                .ToList();
        }

        var selectedOptionId = answer.SelectedOptionIds.FirstOrDefault();
        if (selectedOptionId == Guid.Empty)
        {
            return Array.Empty<ResponseAnswer>();
        }

        return new[]
        {
            new ResponseAnswer
            {
                Id = Guid.NewGuid(),
                ResponseId = responseId,
                QuestionId = question.Id,
                OptionId = selectedOptionId,
            },
        };
    }

    private async Task<IReadOnlyCollection<ParticipationAnswerDto>> LoadDraftAnswersAsync(
        Guid sessionId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var draftResponse = await this.dbContext.Responses
            .Include(response => response.Answers)
            .Where(response => response.SessionParticipant.SessionId == sessionId)
            .Where(response => response.SessionParticipant.UserId == userId)
            .OrderByDescending(r => r.IsDraft)
            .FirstOrDefaultAsync(cancellationToken);

        if (draftResponse == null)
        {
            return Array.Empty<ParticipationAnswerDto>();
        }

        return draftResponse.Answers
            .GroupBy(answer => answer.QuestionId)
            .Select(group => new ParticipationAnswerDto
            {
                QuestionId = group.Key,
                TextAnswer = group.FirstOrDefault(answer => !string.IsNullOrWhiteSpace(answer.TextAnswer))?.TextAnswer,
                SelectedOptionIds = group
                    .Where(answer => answer.OptionId.HasValue)
                    .Select(answer => answer.OptionId!.Value)
                    .Distinct()
                    .ToList(),
            })
            .ToList();
    }

    private async Task<bool> IsAlreadySubmittedAsync(Guid sessionId, Guid userId, CancellationToken ct)
    {
        return await this.dbContext.Responses
            .AnyAsync(
                r =>
                !r.IsDraft &&
                r.SessionParticipant.SessionId == sessionId &&
                r.SessionParticipant.UserId == userId,
                ct);
    }

    private async Task QueueSurveyResponseSubmittedNotificationAsync(
        Survey survey,
        Guid respondentUserId,
        CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = survey.AuthorId,
            Type = NotificationType.SurveyResponseSubmitted,
            Title = "New response submitted",
            Message = $"A new response was submitted for '{survey.Title}' by user {respondentUserId}.",
            RelatedEntityId = survey.Id,
            CreatedAt = DateTime.UtcNow,
        };

        await this.dbContext.Notifications.AddAsync(notification, cancellationToken);
        await this.dbContext.SaveChangesAsync(cancellationToken);
    }
}
