// <copyright file="AdminSurveyService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SurveyPro.Application.DTOs.Questions;
using SurveyPro.Application.DTOs.Surveys;
using SurveyPro.Application.Interfaces;
using SurveyPro.Infrastructure.Persistence;
using SurveyPro.Domain.Enums;
using SurveyPro.Application.Common;

/// <summary>
/// Admin survey use-cases: view all surveys and delete any survey.
/// </summary>
public sealed class AdminSurveyService : IAdminSurveyService
{
    private readonly SurveyProDbContext dbContext;
    private readonly ILogger<AdminSurveyService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminSurveyService"/> class.
    /// </summary>
    /// <param name="dbContext">Database context.</param>
    /// <param name="logger">Logger instance.</param>
    public AdminSurveyService(SurveyProDbContext dbContext, ILogger<AdminSurveyService> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<AdminSurveyListItemDto>> GetAllSurveysAsync(CancellationToken cancellationToken)
    {
        var surveys = await this.dbContext.Surveys
            .AsNoTracking()
            .Where(s => s.Status != SurveyStatuses.Draft)
            .Include(s => s.Author)
            .Include(s => s.Questions)
            .Include(s => s.Sessions)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        var surveyIds = surveys.Select(s => s.Id).ToList();

        var responseCounts = await this.dbContext.Responses
            .AsNoTracking()
            .Where(r => !r.IsDraft && surveyIds.Contains(r.SessionParticipant.Session.SurveyId))
            .GroupBy(r => r.SessionParticipant.Session.SurveyId)
            .Select(g => new { SurveyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SurveyId, x => x.Count, cancellationToken);

        var accessCodes = await this.dbContext.SurveySessions
            .AsNoTracking()
            .Where(s => s.IsActive && surveyIds.Contains(s.SurveyId))
            .GroupBy(s => s.SurveyId)
            .Select(g => new { SurveyId = g.Key, Code = g.OrderByDescending(s => s.CreatedAt).First().AccessCode })
            .ToDictionaryAsync(x => x.SurveyId, x => x.Code, cancellationToken);

        return surveys.Select(s => new AdminSurveyListItemDto
        {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            Status = s.Status,
            IsPublic = s.IsPublic,
            CreatedAt = s.CreatedAt,
            AccessCode = accessCodes.TryGetValue(s.Id, out var code) ? code : string.Empty,
            AuthorName = s.Author?.Name ?? string.Empty,
            AuthorEmail = s.Author?.Email ?? string.Empty,
            QuestionCount = s.Questions?.Count ?? 0,
            ResponseCount = responseCounts.TryGetValue(s.Id, out var count) ? count : 0,
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteSurveyAsync(Guid surveyId, CancellationToken cancellationToken)
    {
        var survey = await this.dbContext.Surveys
            .FirstOrDefaultAsync(s => s.Id == surveyId, cancellationToken);

        if (survey == null)
        {
            return false;
        }

        this.dbContext.Surveys.Remove(survey);
        await this.dbContext.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Admin deleted survey {SurveyId} titled '{Title}'", surveyId, survey.Title);

        return true;
    }

    /// <inheritdoc/>
    public async Task<Result<AdminSurveyQuestionsDto>> GetSurveyQuestionsAsync(Guid surveyId, CancellationToken cancellationToken)
    {
        var survey = await this.dbContext.Surveys
            .AsNoTracking()
            .Include(s => s.Author)
            .Include(s => s.Questions)
                .ThenInclude(q => q.Options)
            .Include(s => s.Sessions)
            .FirstOrDefaultAsync(s => s.Id == surveyId, cancellationToken);

        if (survey == null)
        {
            return Result<AdminSurveyQuestionsDto>.Failure("Survey not found.");
        }

        return Result<AdminSurveyQuestionsDto>.Success(new AdminSurveyQuestionsDto
        {
            SurveyId = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            Status = survey.Status,
            IsPublic = survey.IsPublic,
            CreatedAt = survey.CreatedAt,
            AccessCode = survey.Sessions
                .Where(session => session.IsActive)
                .OrderByDescending(session => session.CreatedAt)
                .Select(session => session.AccessCode)
                .FirstOrDefault() ?? string.Empty,
            AuthorName = survey.Author?.Name ?? string.Empty,
            AuthorEmail = survey.Author?.Email ?? string.Empty,
            Questions = survey.Questions
                .OrderBy(question => question.OrderNumber)
                .Select(question => new QuestionDto
                {
                    Id = question.Id,
                    SurveyId = question.SurveyId,
                    Text = question.Text,
                    Type = question.Type,
                    OrderNumber = question.OrderNumber,
                    Options = question.Options
                        .OrderBy(option => option.Text)
                        .Select(option => option.Text)
                        .ToList(),
                })
                .ToList(),
        });
    }

    /// <inheritdoc/>
    public async Task<Result<AdminSurveyResponsesDto>> GetSurveyResponsesAsync(Guid surveyId, CancellationToken cancellationToken)
    {
        var survey = await this.dbContext.Surveys
            .AsNoTracking()
            .Include(s => s.Sessions)
                .ThenInclude(session => session.Participants)
                    .ThenInclude(participant => participant.Responses)
                        .ThenInclude(response => response.Answers)
                            .ThenInclude(answer => answer.Question)
            .Include(s => s.Sessions)
                .ThenInclude(session => session.Participants)
                    .ThenInclude(participant => participant.Responses)
                        .ThenInclude(response => response.Answers)
                            .ThenInclude(answer => answer.Option)
            .Include(s => s.Sessions)
                .ThenInclude(session => session.Participants)
                    .ThenInclude(participant => participant.User)
            .FirstOrDefaultAsync(s => s.Id == surveyId, cancellationToken);

        if (survey == null)
        {
            return Result<AdminSurveyResponsesDto>.Failure("Survey not found.");
        }

        var participants = survey.Sessions
            .SelectMany(session => session.Participants)
            .Where(participant => participant.Responses.Any(r => !r.IsDraft))
            .Select(participant =>
            {
                var latestResponse = participant.Responses
                    .Where(r => !r.IsDraft)
                    .OrderByDescending(r => r.SubmittedAt ?? r.CreatedAt)
                    .First();

                return new AdminParticipantResponseDto
                {
                    ParticipantId = participant.Id,
                    ParticipantName = participant.User?.Name ?? "Anonymous",
                    SubmittedAt = latestResponse.SubmittedAt ?? latestResponse.CreatedAt,

                    Answers = latestResponse.Answers
                        .OrderBy(a => a.Question.OrderNumber)
                        .Select(a => new AdminQuestionAnswerDto
                        {
                            QuestionId = a.QuestionId,
                            QuestionText = a.Question.Text,
                            QuestionType = a.Question.Type.ToString(),
                            Answer = !string.IsNullOrWhiteSpace(a.TextAnswer)
                                ? a.TextAnswer
                                : a.Option != null
                                    ? a.Option.Text
                                    : "No answer",
                        })
                        .ToList(),
                };
            })
            .OrderByDescending(p => p.SubmittedAt)
            .ToList();

        return Result<AdminSurveyResponsesDto>.Success(new AdminSurveyResponsesDto
        {
            SurveyId = survey.Id,
            SurveyTitle = survey.Title,
            TotalParticipants = participants.Count,
            Participants = participants,
        });
    }
}
