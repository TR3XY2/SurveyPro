// <copyright file="AdminSurveyServiceExtendedTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Tests.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyPro.Domain.Entities;
using SurveyPro.Domain.Enums;
using SurveyPro.Infrastructure.Persistence;
using SurveyPro.Infrastructure.Services;
using Xunit;

/// <summary>
/// Tests for <see cref="AdminSurveyService"/> methods:
/// GetSurveyQuestionsAsync, GetSurveyResponsesAsync, DeleteParticipantResponseAsync.
/// </summary>
public class AdminSurveyServiceExtendedTests
{
    private static SurveyProDbContext CreateDbContext() =>
        new SurveyProDbContext(
            new DbContextOptionsBuilder<SurveyProDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    private static AdminSurveyService BuildService(SurveyProDbContext db)
    {
        var logger = new Mock<ILogger<AdminSurveyService>>();
        return new AdminSurveyService(db, logger.Object);
    }

    private static async Task<(Survey survey, SurveySession session)> SeedSurveyAsync(
        SurveyProDbContext db,
        Action<Survey>? configure = null)
    {
        var author = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Name = "Admin Author",
            Email = "admin@test.com",
        };

        var survey = new Survey
        {
            Id = Guid.NewGuid(),
            AuthorId = author.Id,
            Author = author,
            Title = "Test Survey",
            Description = "Test Description",
            Status = SurveyStatuses.Published,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
        };

        configure?.Invoke(survey);

        var session = new SurveySession
        {
            Id = Guid.NewGuid(),
            SurveyId = survey.Id,
            AccessCode = "CODE0001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        await db.Users.AddAsync(author);
        await db.Surveys.AddAsync(survey);
        await db.SurveySessions.AddAsync(session);
        await db.SaveChangesAsync();

        return (survey, session);
    }

    private static async Task<Question> SeedQuestionAsync(
        SurveyProDbContext db,
        Guid surveyId,
        string type = "Text",
        int order = 1,
        List<string>? options = null)
    {
        var question = new Question
        {
            Id = Guid.NewGuid(),
            SurveyId = surveyId,
            Text = $"Question {order}",
            Type = type,
            OrderNumber = order,
        };

        if (options != null)
        {
            question.Options = options.Select(o => new AnswerOption
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                Text = o,
            }).ToList();
        }

        await db.Questions.AddAsync(question);
        await db.SaveChangesAsync();

        return question;
    }

    /// <summary>Seeds a participant with one submitted response that answers the given question.</summary>
    private static async Task<(SessionParticipant participant, Response response)> SeedSubmittedResponseAsync(
        SurveyProDbContext db,
        Guid sessionId,
        Question question,
        string textAnswer = "Blue",
        string participantName = "Respondent")
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Name = participantName,
            Email = $"{participantName.ToLower()}@test.com",
        };

        var participant = new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = user.Id,
            User = user,
            JoinedAt = DateTime.UtcNow,
        };

        var answer = new ResponseAnswer
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            Question = question,
            TextAnswer = question.Type == "Text" ? textAnswer : null,
        };

        var response = new Response
        {
            Id = Guid.NewGuid(),
            SessionParticipantId = participant.Id,
            IsDraft = false,
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
            Answers = new List<ResponseAnswer> { answer },
        };

        await db.Users.AddAsync(user);
        await db.SessionParticipants.AddAsync(participant);
        await db.Responses.AddAsync(response);
        await db.SaveChangesAsync();

        return (participant, response);
    }

    [Fact]
    public async Task GetSurveyQuestionsAsync_SurveyNotFound_ReturnsFailure()
    {
        var db = CreateDbContext();
        var service = BuildService(db);

        var result = await service.GetSurveyQuestionsAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Survey not found.");
    }

    [Fact]
    public async Task GetSurveyQuestionsAsync_ValidId_ReturnsSuccess()
    {
        var db = CreateDbContext();
        var (survey, _) = await SeedSurveyAsync(db);
        var service = BuildService(db);

        var result = await service.GetSurveyQuestionsAsync(survey.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetSurveyQuestionsAsync_MapsBasicSurveyFields()
    {
        var db = CreateDbContext();
        var (survey, _) = await SeedSurveyAsync(db);
        var service = BuildService(db);

        var result = await service.GetSurveyQuestionsAsync(survey.Id, CancellationToken.None);
        var dto = result.Value!;

        dto.SurveyId.Should().Be(survey.Id);
        dto.Title.Should().Be(survey.Title);
        dto.Description.Should().Be(survey.Description);
        dto.Status.Should().Be(survey.Status);
        dto.IsPublic.Should().Be(survey.IsPublic);
    }

    [Fact]
    public async Task GetSurveyQuestionsAsync_MapsAuthorFields()
    {
        var db = CreateDbContext();
        var (survey, _) = await SeedSurveyAsync(db);
        var service = BuildService(db);

        var result = await service.GetSurveyQuestionsAsync(survey.Id, CancellationToken.None);
        var dto = result.Value!;

        dto.AuthorName.Should().Be("Admin Author");
        dto.AuthorEmail.Should().Be("admin@test.com");
    }

    [Fact]
    public async Task GetSurveyQuestionsAsync_MapsActiveSessionAccessCode()
    {
        var db = CreateDbContext();
        var (survey, _) = await SeedSurveyAsync(db);
        var service = BuildService(db);

        var result = await service.GetSurveyQuestionsAsync(survey.Id, CancellationToken.None);

        result.Value!.AccessCode.Should().Be("CODE0001");
    }

    [Fact]
    public async Task GetSurveyQuestionsAsync_NoActiveSession_ReturnsEmptyAccessCode()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);

        session.IsActive = false;
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var result = await service.GetSurveyQuestionsAsync(survey.Id, CancellationToken.None);

        result.Value!.AccessCode.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSurveyQuestionsAsync_NoQuestions_ReturnsEmptyList()
    {
        var db = CreateDbContext();
        var (survey, _) = await SeedSurveyAsync(db);
        var service = BuildService(db);

        var result = await service.GetSurveyQuestionsAsync(survey.Id, CancellationToken.None);

        result.Value!.Questions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSurveyQuestionsAsync_QuestionsOrderedByOrderNumber()
    {
        var db = CreateDbContext();
        var (survey, _) = await SeedSurveyAsync(db);

        await SeedQuestionAsync(db, survey.Id, order: 2);
        await SeedQuestionAsync(db, survey.Id, order: 1);

        var service = BuildService(db);
        var result = await service.GetSurveyQuestionsAsync(survey.Id, CancellationToken.None);

        result.Value!.Questions
            .Select(q => q.OrderNumber)
            .Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetSurveyQuestionsAsync_MapsQuestionFields()
    {
        var db = CreateDbContext();
        var (survey, _) = await SeedSurveyAsync(db);
        var question = await SeedQuestionAsync(db, survey.Id, type: "Text", order: 1);
        var service = BuildService(db);

        var result = await service.GetSurveyQuestionsAsync(survey.Id, CancellationToken.None);
        var dto = result.Value!.Questions.Single();

        dto.Id.Should().Be(question.Id);
        dto.SurveyId.Should().Be(survey.Id);
        dto.Text.Should().Be(question.Text);
        dto.Type.Should().Be("Text");
        dto.OrderNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetSurveyQuestionsAsync_ChoiceQuestion_MapsOptionsAlphabetically()
    {
        var db = CreateDbContext();
        var (survey, _) = await SeedSurveyAsync(db);
        await SeedQuestionAsync(
            db, survey.Id,
            type: "SingleChoice",
            order: 1,
            options: new List<string> { "Zebra", "Apple", "Mango" });

        var service = BuildService(db);
        var result = await service.GetSurveyQuestionsAsync(survey.Id, CancellationToken.None);

        result.Value!.Questions.Single().Options
            .Should().BeEquivalentTo(
                new[] { "Apple", "Mango", "Zebra" },
                opts => opts.WithStrictOrdering());
    }

    [Fact]
    public async Task GetSurveyQuestionsAsync_TextQuestion_HasEmptyOptions()
    {
        var db = CreateDbContext();
        var (survey, _) = await SeedSurveyAsync(db);
        await SeedQuestionAsync(db, survey.Id, type: "Text", order: 1);

        var service = BuildService(db);
        var result = await service.GetSurveyQuestionsAsync(survey.Id, CancellationToken.None);

        result.Value!.Questions.Single().Options.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSurveyResponsesAsync_SurveyNotFound_ReturnsFailure()
    {
        var db = CreateDbContext();
        var service = BuildService(db);

        var result = await service.GetSurveyResponsesAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Survey not found.");
    }

    [Fact]
    public async Task GetSurveyResponsesAsync_ValidId_ReturnsSuccess()
    {
        var db = CreateDbContext();
        var (survey, _) = await SeedSurveyAsync(db);
        var service = BuildService(db);

        var result = await service.GetSurveyResponsesAsync(survey.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetSurveyResponsesAsync_MapsBasicSurveyFields()
    {
        var db = CreateDbContext();
        var (survey, _) = await SeedSurveyAsync(db);
        var service = BuildService(db);

        var result = await service.GetSurveyResponsesAsync(survey.Id, CancellationToken.None);
        var dto = result.Value!;

        dto.SurveyId.Should().Be(survey.Id);
        dto.SurveyTitle.Should().Be(survey.Title);
    }

    [Fact]
    public async Task GetSurveyResponsesAsync_NoSubmittedResponses_TotalParticipantsIsZero()
    {
        var db = CreateDbContext();
        var (survey, _) = await SeedSurveyAsync(db);
        var service = BuildService(db);

        var result = await service.GetSurveyResponsesAsync(survey.Id, CancellationToken.None);

        result.Value!.TotalParticipants.Should().Be(0);
        result.Value.Participants.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSurveyResponsesAsync_DraftResponsesAreExcluded()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);
        var question = await SeedQuestionAsync(db, survey.Id);

        var user = new ApplicationUser { Id = Guid.NewGuid(), Name = "Drafter", Email = "d@d.com" };
        var participant = new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            UserId = user.Id,
            JoinedAt = DateTime.UtcNow,
        };
        var draft = new Response
        {
            Id = Guid.NewGuid(),
            SessionParticipantId = participant.Id,
            IsDraft = true,
            CreatedAt = DateTime.UtcNow,
            Answers = new List<ResponseAnswer>
            {
                new ResponseAnswer { Id = Guid.NewGuid(), QuestionId = question.Id, TextAnswer = "Draft" },
            },
        };

        await db.Users.AddAsync(user);
        await db.SessionParticipants.AddAsync(participant);
        await db.Responses.AddAsync(draft);
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var result = await service.GetSurveyResponsesAsync(survey.Id, CancellationToken.None);

        result.Value!.TotalParticipants.Should().Be(0);
    }

    [Fact]
    public async Task GetSurveyResponsesAsync_OneSubmittedResponse_TotalParticipantsIsOne()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);
        var question = await SeedQuestionAsync(db, survey.Id);
        await SeedSubmittedResponseAsync(db, session.Id, question);

        var service = BuildService(db);
        var result = await service.GetSurveyResponsesAsync(survey.Id, CancellationToken.None);

        result.Value!.TotalParticipants.Should().Be(1);
    }

    [Fact]
    public async Task GetSurveyResponsesAsync_MapsParticipantName()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);
        var question = await SeedQuestionAsync(db, survey.Id);
        await SeedSubmittedResponseAsync(db, session.Id, question, participantName: "Alice");

        var service = BuildService(db);
        var result = await service.GetSurveyResponsesAsync(survey.Id, CancellationToken.None);

        result.Value!.Participants.Single().ParticipantName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetSurveyResponsesAsync_TextAnswer_MappedCorrectly()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);
        var question = await SeedQuestionAsync(db, survey.Id, type: "Text");
        await SeedSubmittedResponseAsync(db, session.Id, question, textAnswer: "Red");

        var service = BuildService(db);
        var result = await service.GetSurveyResponsesAsync(survey.Id, CancellationToken.None);

        result.Value!.Participants.Single()
            .Answers.Single().Answer.Should().Be("Red");
    }

    [Fact]
    public async Task GetSurveyResponsesAsync_NullTextAndNullOption_AnswerIsNoAnswer()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);
        var question = await SeedQuestionAsync(db, survey.Id, type: "SingleChoice");

        var user = new ApplicationUser { Id = Guid.NewGuid(), Name = "Bob", Email = "b@b.com" };
        var participant = new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            UserId = user.Id,
            JoinedAt = DateTime.UtcNow,
        };
        var response = new Response
        {
            Id = Guid.NewGuid(),
            SessionParticipantId = participant.Id,
            IsDraft = false,
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
            Answers = new List<ResponseAnswer>
            {
                new ResponseAnswer
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    Question = question,
                    TextAnswer = null,
                    OptionId = null,
                    Option = null,
                },
            },
        };

        await db.Users.AddAsync(user);
        await db.SessionParticipants.AddAsync(participant);
        await db.Responses.AddAsync(response);
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var result = await service.GetSurveyResponsesAsync(survey.Id, CancellationToken.None);

        result.Value!.Participants.Single()
            .Answers.Single().Answer.Should().Be("No answer");
    }

    [Fact]
    public async Task GetSurveyResponsesAsync_OptionAnswer_MapsOptionText()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);

        var option = new AnswerOption { Id = Guid.NewGuid(), Text = "Yes" };
        var question = new Question
        {
            Id = Guid.NewGuid(),
            SurveyId = survey.Id,
            Text = "Do you agree?",
            Type = "SingleChoice",
            OrderNumber = 1,
            Options = new List<AnswerOption> { option },
        };
        option.QuestionId = question.Id;

        await db.Questions.AddAsync(question);
        await db.SaveChangesAsync();

        var user = new ApplicationUser { Id = Guid.NewGuid(), Name = "Carol", Email = "c@c.com" };
        var participant = new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            UserId = user.Id,
            JoinedAt = DateTime.UtcNow,
        };
        var response = new Response
        {
            Id = Guid.NewGuid(),
            SessionParticipantId = participant.Id,
            IsDraft = false,
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
            Answers = new List<ResponseAnswer>
            {
                new ResponseAnswer
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    Question = question,
                    OptionId = option.Id,
                    Option = option,
                },
            },
        };

        await db.Users.AddAsync(user);
        await db.SessionParticipants.AddAsync(participant);
        await db.Responses.AddAsync(response);
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var result = await service.GetSurveyResponsesAsync(survey.Id, CancellationToken.None);

        result.Value!.Participants.Single()
            .Answers.Single().Answer.Should().Be("Yes");
    }

    [Fact]
    public async Task GetSurveyResponsesAsync_MultipleParticipants_OrderedBySubmittedAtDescending()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);
        var question = await SeedQuestionAsync(db, survey.Id);

        // First participant submitted earlier
        var (p1, r1) = await SeedSubmittedResponseAsync(db, session.Id, question, participantName: "Earlier");
        var (p2, r2) = await SeedSubmittedResponseAsync(db, session.Id, question, participantName: "Later");

        r1.SubmittedAt = DateTime.UtcNow.AddHours(-2);
        r2.SubmittedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var result = await service.GetSurveyResponsesAsync(survey.Id, CancellationToken.None);

        result.Value!.Participants.First().ParticipantName.Should().Be("Later");
    }

    [Fact]
    public async Task GetSurveyResponsesAsync_AnswersOrderedByQuestionOrderNumber()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);

        var q1 = await SeedQuestionAsync(db, survey.Id, order: 1);
        var q2 = await SeedQuestionAsync(db, survey.Id, order: 2);

        var user = new ApplicationUser { Id = Guid.NewGuid(), Name = "Dave", Email = "d@d.com" };
        var participant = new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            UserId = user.Id,
            JoinedAt = DateTime.UtcNow,
        };
        var response = new Response
        {
            Id = Guid.NewGuid(),
            SessionParticipantId = participant.Id,
            IsDraft = false,
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
            Answers = new List<ResponseAnswer>
            {
                new ResponseAnswer { Id = Guid.NewGuid(), QuestionId = q2.Id, Question = q2, TextAnswer = "A2" },
                new ResponseAnswer { Id = Guid.NewGuid(), QuestionId = q1.Id, Question = q1, TextAnswer = "A1" },
            },
        };

        await db.Users.AddAsync(user);
        await db.SessionParticipants.AddAsync(participant);
        await db.Responses.AddAsync(response);
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var result = await service.GetSurveyResponsesAsync(survey.Id, CancellationToken.None);

        result.Value!.Participants.Single().Answers
            .Select(a => a.QuestionText)
            .Should().BeEquivalentTo(new[] { q1.Text, q2.Text }, o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task GetSurveyResponsesAsync_MapsQuestionTypeOnAnswer()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);
        var question = await SeedQuestionAsync(db, survey.Id, type: "Text");
        await SeedSubmittedResponseAsync(db, session.Id, question);

        var service = BuildService(db);
        var result = await service.GetSurveyResponsesAsync(survey.Id, CancellationToken.None);

        result.Value!.Participants.Single()
            .Answers.Single().QuestionType.Should().Be("Text");
    }

    [Fact]
    public async Task GetSurveyResponsesAsync_ParticipantWithOnlyDrafts_IsExcluded()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);
        var question = await SeedQuestionAsync(db, survey.Id);

        var user = new ApplicationUser { Id = Guid.NewGuid(), Name = "Ghost", Email = "g@g.com" };
        var participant = new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            UserId = user.Id,
            JoinedAt = DateTime.UtcNow,
        };
        var draftOnly = new Response
        {
            Id = Guid.NewGuid(),
            SessionParticipantId = participant.Id,
            IsDraft = true,
            CreatedAt = DateTime.UtcNow,
            Answers = new List<ResponseAnswer>
            {
                new ResponseAnswer { Id = Guid.NewGuid(), QuestionId = question.Id, TextAnswer = "draft" },
            },
        };

        await db.Users.AddAsync(user);
        await db.SessionParticipants.AddAsync(participant);
        await db.Responses.AddAsync(draftOnly);
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var result = await service.GetSurveyResponsesAsync(survey.Id, CancellationToken.None);

        result.Value!.TotalParticipants.Should().Be(0);
    }

    [Fact]
    public async Task DeleteParticipantResponseAsync_ParticipantNotFound_ReturnsFailure()
    {
        var db = CreateDbContext();
        var service = BuildService(db);

        var result = await service.DeleteParticipantResponseAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Participant not found");
    }

    [Fact]
    public async Task DeleteParticipantResponseAsync_ValidParticipant_ReturnsSuccess()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);
        var question = await SeedQuestionAsync(db, survey.Id);
        var (participant, _) = await SeedSubmittedResponseAsync(db, session.Id, question);

        var service = BuildService(db);
        var result = await service.DeleteParticipantResponseAsync(participant.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteParticipantResponseAsync_ValidParticipant_RemovesAllResponses()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);
        var question = await SeedQuestionAsync(db, survey.Id);
        var (participant, _) = await SeedSubmittedResponseAsync(db, session.Id, question);

        var service = BuildService(db);
        await service.DeleteParticipantResponseAsync(participant.Id, CancellationToken.None);

        var remaining = await db.Responses
            .Where(r => r.SessionParticipantId == participant.Id)
            .ToListAsync();

        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteParticipantResponseAsync_ValidParticipant_RemovesAllAnswers()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);
        var question = await SeedQuestionAsync(db, survey.Id);
        var (participant, response) = await SeedSubmittedResponseAsync(db, session.Id, question);

        var service = BuildService(db);
        await service.DeleteParticipantResponseAsync(participant.Id, CancellationToken.None);

        var remaining = await db.ResponseAnswers
            .Where(a => a.ResponseId == response.Id)
            .ToListAsync();

        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteParticipantResponseAsync_MultipleResponses_RemovesAll()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);
        var question = await SeedQuestionAsync(db, survey.Id);

        var user = new ApplicationUser { Id = Guid.NewGuid(), Name = "Eve", Email = "e@e.com" };
        var participant = new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            UserId = user.Id,
            JoinedAt = DateTime.UtcNow,
        };

        var r1 = new Response
        {
            Id = Guid.NewGuid(),
            SessionParticipantId = participant.Id,
            IsDraft = true,
            CreatedAt = DateTime.UtcNow,
            Answers = new List<ResponseAnswer>
            {
                new ResponseAnswer { Id = Guid.NewGuid(), QuestionId = question.Id, TextAnswer = "Draft answer" },
            },
        };
        var r2 = new Response
        {
            Id = Guid.NewGuid(),
            SessionParticipantId = participant.Id,
            IsDraft = false,
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
            Answers = new List<ResponseAnswer>
            {
                new ResponseAnswer { Id = Guid.NewGuid(), QuestionId = question.Id, TextAnswer = "Final answer" },
            },
        };

        await db.Users.AddAsync(user);
        await db.SessionParticipants.AddAsync(participant);
        await db.Responses.AddRangeAsync(r1, r2);
        await db.SaveChangesAsync();

        var service = BuildService(db);
        await service.DeleteParticipantResponseAsync(participant.Id, CancellationToken.None);

        db.Responses.Should().BeEmpty();
        db.ResponseAnswers.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteParticipantResponseAsync_DoesNotAffectOtherParticipants()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);
        var question = await SeedQuestionAsync(db, survey.Id);

        var (target, _) = await SeedSubmittedResponseAsync(db, session.Id, question, participantName: "Target");
        var (other, _) = await SeedSubmittedResponseAsync(db, session.Id, question, participantName: "Other");

        var service = BuildService(db);
        await service.DeleteParticipantResponseAsync(target.Id, CancellationToken.None);

        var otherResponses = await db.Responses
            .Where(r => r.SessionParticipantId == other.Id)
            .ToListAsync();

        otherResponses.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteParticipantResponseAsync_ParticipantWithNoResponses_ReturnsSuccess()
    {
        var db = CreateDbContext();
        var (survey, session) = await SeedSurveyAsync(db);

        var user = new ApplicationUser { Id = Guid.NewGuid(), Name = "Empty", Email = "e@e.com" };
        var participant = new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            UserId = user.Id,
            JoinedAt = DateTime.UtcNow,
        };

        await db.Users.AddAsync(user);
        await db.SessionParticipants.AddAsync(participant);
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var result = await service.DeleteParticipantResponseAsync(participant.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}