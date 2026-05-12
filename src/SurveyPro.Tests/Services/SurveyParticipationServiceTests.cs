// <copyright file="SurveyParticipationServiceTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Tests.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyPro.Application.DTOs.Participation;
using SurveyPro.Infrastructure.Services;
using SurveyPro.Domain.Entities;
using SurveyPro.Domain.Enums;
using SurveyPro.Infrastructure.Persistence;
using Xunit;

/// <summary>
/// Unit tests for <see cref="SurveyParticipationService"/>.
/// </summary>
public class SurveyParticipationServiceTests
{
    private static readonly Guid ValidSurveyId = Guid.NewGuid();
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private const string ValidCode = "ABCD1234";
    private const string SurveyNotPublishedMessage = "This survey is being configured and is not available yet.";

    private static SurveyProDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SurveyProDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SurveyProDbContext(options);
    }

    private static ILogger<SurveyParticipationService> CreateMockLogger()
    {
        var mock = new Mock<ILogger<SurveyParticipationService>>();
        return mock.Object;
    }

    private static async Task SeedSessionAsync(
        SurveyProDbContext dbContext,
        SurveyStatuses status,
        string accessCode = ValidCode)
    {
        var survey = new Survey
        {
            Id = ValidSurveyId,
            Title = "Test Survey",
            Status = status,
            IsPublic = true,
            AuthorId = Guid.NewGuid(),
        };

        var session = new SurveySession
        {
            Id = Guid.NewGuid(),
            SurveyId = survey.Id,
            Survey = survey,
            AccessCode = accessCode,
            IsActive = true,
        };

        await dbContext.Surveys.AddAsync(survey);
        await dbContext.SurveySessions.AddAsync(session);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByCodeAsync_NullCode_ReturnsFailure()
    {
        var dbContext = CreateDbContext();
        var logger = CreateMockLogger();
        var service = new SurveyParticipationService(dbContext, logger);

        var result = await service.GetByCodeAsync(null!, null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GetByCodeAsync_InvalidCode_ReturnsFailure()
    {
        var dbContext = CreateDbContext();
        var logger = CreateMockLogger();
        var service = new SurveyParticipationService(dbContext, logger);

        var result = await service.GetByCodeAsync("INVALID", null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Survey not found");
    }

    [Fact]
    public async Task GetByCodeAsync_UnpublishedSurvey_ReturnsConfiguredMessage()
    {
        var dbContext = CreateDbContext();
        await SeedSessionAsync(dbContext, SurveyStatuses.Draft);

        var logger = CreateMockLogger();
        var service = new SurveyParticipationService(dbContext, logger);

        var result = await service.GetByCodeAsync(ValidCode, null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SurveyNotPublishedMessage);
    }

    [Fact]
    public async Task SaveDraftAsync_InvalidUserId_ReturnsFailure()
    {
        var dbContext = CreateDbContext();
        var logger = CreateMockLogger();
        var service = new SurveyParticipationService(dbContext, logger);

        var request = new SaveDraftRequestDto
        {
            SurveyId = ValidSurveyId,
            AccessCode = ValidCode,
            Answers = new List<ParticipationAnswerDto>(),
        };

        var result = await service.SaveDraftAsync(Guid.Empty, request, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task SaveDraftAsync_NullCode_ReturnsFailure()
    {
        var dbContext = CreateDbContext();
        var logger = CreateMockLogger();
        var service = new SurveyParticipationService(dbContext, logger);

        var request = new SaveDraftRequestDto
        {
            SurveyId = ValidSurveyId,
            AccessCode = null!,
            Answers = new List<ParticipationAnswerDto>(),
        };

        var result = await service.SaveDraftAsync(ValidUserId, request, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task SaveDraftAsync_UnpublishedSurvey_ReturnsConfiguredMessage()
    {
        // Arrange
        var dbContext = CreateDbContext();
        await SeedSessionAsync(dbContext, SurveyStatuses.Draft);

        var logger = CreateMockLogger();
        var service = new SurveyParticipationService(dbContext, logger);

        var request = new SaveDraftRequestDto
        {
            SurveyId = ValidSurveyId,
            AccessCode = ValidCode,
            Answers = new List<ParticipationAnswerDto>(),
        };

        // Act
        var result = await service.SaveDraftAsync(ValidUserId, request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SurveyNotPublishedMessage);
    }

    [Fact]
    public async Task ClearDraftAsync_InvalidSession_ReturnsFailureWithoutThrowing()
    {
        var dbContext = CreateDbContext();
        var logger = CreateMockLogger();
        var service = new SurveyParticipationService(dbContext, logger);

        var result = await service.ClearDraftAsync(ValidUserId, "INVALID", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAsync_InvalidSession_ReturnsFailure()
    {
        var dbContext = CreateDbContext();
        var logger = CreateMockLogger();
        var service = new SurveyParticipationService(dbContext, logger);

        var result = await service.SubmitAsync(ValidUserId, "INVALID", ValidSurveyId, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Survey not found");
    }

    [Fact]
    public async Task SubmitAsync_UnpublishedSurvey_ReturnsConfiguredMessage()
    {
        var dbContext = CreateDbContext();
        await SeedSessionAsync(dbContext, SurveyStatuses.Draft);

        var logger = CreateMockLogger();
        var service = new SurveyParticipationService(dbContext, logger);

        var result = await service.SubmitAsync(ValidUserId, ValidCode, ValidSurveyId, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SurveyNotPublishedMessage);
    }
}

/// <summary>
/// Extended integration-style tests for <see cref="SurveyParticipationService"/>
/// using an in-memory database.
/// </summary>
public class SurveyParticipationServiceExtendedTests
{
    private const string ValidCode = "HAPPY001";
    private static readonly Guid SurveyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static SurveyProDbContext CreateDbContext() =>
        new SurveyProDbContext(
            new DbContextOptionsBuilder<SurveyProDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    private static SurveyParticipationService BuildService(SurveyProDbContext db) =>
        new SurveyParticipationService(db, new Mock<ILogger<SurveyParticipationService>>().Object);

    /// <summary>Seeds a published survey with one text question and an active session.</summary>
    private static async Task<(Survey survey, SurveySession session, Question question)> SeedPublishedAsync(
        SurveyProDbContext db,
        string code = ValidCode,
        Guid? surveyId = null)
    {
        var sid = surveyId ?? SurveyId;

        var question = new Question
        {
            Id = Guid.NewGuid(),
            SurveyId = sid,
            Text = "Favourite colour?",
            Type = "Text",
            OrderNumber = 1,
        };

        var survey = new Survey
        {
            Id = sid,
            Title = "Happy Survey",
            Status = SurveyStatuses.Published,
            IsPublic = true,
            AuthorId = Guid.NewGuid(),
            Questions = new List<Question> { question },
        };

        var session = new SurveySession
        {
            Id = Guid.NewGuid(),
            SurveyId = survey.Id,
            Survey = survey,
            AccessCode = code,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        await db.Surveys.AddAsync(survey);
        await db.SurveySessions.AddAsync(session);
        await db.SaveChangesAsync();

        return (survey, session, question);
    }

    [Fact]
    public async Task GetByCodeAsync_PublishedSurvey_ReturnsSuccess()
    {
        var db = CreateDbContext();
        await SeedPublishedAsync(db);
        var service = BuildService(db);

        var result = await service.GetByCodeAsync(ValidCode, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetByCodeAsync_PublishedSurvey_ReturnsSurveyId()
    {
        var db = CreateDbContext();
        await SeedPublishedAsync(db);
        var service = BuildService(db);

        var result = await service.GetByCodeAsync(ValidCode, null, CancellationToken.None);

        result.Value!.SurveyId.Should().Be(SurveyId);
    }

    [Fact]
    public async Task GetByCodeAsync_PublishedSurvey_ReturnsQuestions()
    {
        var db = CreateDbContext();
        await SeedPublishedAsync(db);
        var service = BuildService(db);

        var result = await service.GetByCodeAsync(ValidCode, null, CancellationToken.None);

        result.Value!.Questions.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByCodeAsync_WithUserId_LoadsDraftAnswers()
    {
        var db = CreateDbContext();
        var (_, session, question) = await SeedPublishedAsync(db);

        var participant = new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            UserId = UserId,
            JoinedAt = DateTime.UtcNow,
        };
        var response = new Response
        {
            Id = Guid.NewGuid(),
            SessionParticipantId = participant.Id,
            IsDraft = true,
            CreatedAt = DateTime.UtcNow,
            Answers = new List<ResponseAnswer>
            {
                new ResponseAnswer
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    TextAnswer = "Blue",
                },
            },
        };
        await db.SessionParticipants.AddAsync(participant);
        await db.Responses.AddAsync(response);
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var result = await service.GetByCodeAsync(ValidCode, UserId, CancellationToken.None);

        result.Value!.DraftAnswers.Should().HaveCount(1);
        result.Value.DraftAnswers.First().TextAnswer.Should().Be("Blue");
    }

    [Fact]
    public async Task GetByCodeAsync_WithCodeHavingLeadingWhitespace_TrimsAndFindsSession()
    {
        var db = CreateDbContext();
        await SeedPublishedAsync(db);
        var service = BuildService(db);

        var result = await service.GetByCodeAsync("  " + ValidCode + "  ", null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SaveDraftAsync_ValidTextAnswer_ReturnsSuccess()
    {
        var db = CreateDbContext();
        var (survey, _, question) = await SeedPublishedAsync(db);
        var service = BuildService(db);

        var request = new SaveDraftRequestDto
        {
            SurveyId = survey.Id,
            AccessCode = ValidCode,
            Answers = new List<ParticipationAnswerDto>
            {
                new() { QuestionId = question.Id, TextAnswer = "Red" },
            },
        };

        var result = await service.SaveDraftAsync(UserId, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SaveDraftAsync_ValidRequest_PersistsDraftInDatabase()
    {
        var db = CreateDbContext();
        var (survey, _, question) = await SeedPublishedAsync(db);
        var service = BuildService(db);

        var request = new SaveDraftRequestDto
        {
            SurveyId = survey.Id,
            AccessCode = ValidCode,
            Answers = new List<ParticipationAnswerDto>
            {
                new() { QuestionId = question.Id, TextAnswer = "Green" },
            },
        };

        await service.SaveDraftAsync(UserId, request, CancellationToken.None);

        var saved = await db.ResponseAnswers.FirstOrDefaultAsync(a => a.TextAnswer == "Green");
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveDraftAsync_CalledTwice_ReplacesAnswers()
    {
        var db = CreateDbContext();
        var (survey, _, question) = await SeedPublishedAsync(db);
        var service = BuildService(db);

        var request1 = new SaveDraftRequestDto
        {
            SurveyId = survey.Id,
            AccessCode = ValidCode,
            Answers = new List<ParticipationAnswerDto>
            {
                new() { QuestionId = question.Id, TextAnswer = "First" },
            },
        };
        var request2 = new SaveDraftRequestDto
        {
            SurveyId = survey.Id,
            AccessCode = ValidCode,
            Answers = new List<ParticipationAnswerDto>
            {
                new() { QuestionId = question.Id, TextAnswer = "Second" },
            },
        };

        await service.SaveDraftAsync(UserId, request1, CancellationToken.None);
        await service.SaveDraftAsync(UserId, request2, CancellationToken.None);

        var allAnswers = db.ResponseAnswers.ToList();
        allAnswers.Should().HaveCount(1);
        allAnswers.First().TextAnswer.Should().Be("Second");
    }

    [Fact]
    public async Task SaveDraftAsync_SurveyMismatch_ReturnsFailure()
    {
        var db = CreateDbContext();
        await SeedPublishedAsync(db);
        var service = BuildService(db);

        var request = new SaveDraftRequestDto
        {
            SurveyId = Guid.NewGuid(),
            AccessCode = ValidCode,
            Answers = Array.Empty<ParticipationAnswerDto>(),
        };

        var result = await service.SaveDraftAsync(UserId, request, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Survey mismatch.");
    }

    [Fact]
    public async Task ClearDraftAsync_NoDraftExists_ReturnsSuccess()
    {
        var db = CreateDbContext();
        await SeedPublishedAsync(db);
        var service = BuildService(db);

        var result = await service.ClearDraftAsync(UserId, ValidCode, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ClearDraftAsync_WithExistingDraft_RemovesAnswers()
    {
        var db = CreateDbContext();
        var (survey, _, question) = await SeedPublishedAsync(db);
        var service = BuildService(db);

        var saveRequest = new SaveDraftRequestDto
        {
            SurveyId = survey.Id,
            AccessCode = ValidCode,
            Answers = new List<ParticipationAnswerDto>
            {
                new() { QuestionId = question.Id, TextAnswer = "To be cleared" },
            },
        };
        await service.SaveDraftAsync(UserId, saveRequest, CancellationToken.None);

        var result = await service.ClearDraftAsync(UserId, ValidCode, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.ResponseAnswers.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearDraftAsync_UnpublishedSurvey_ReturnsFailure()
    {
        var db = CreateDbContext();

        var survey = new Survey
        {
            Id = Guid.NewGuid(),
            Title = "Draft Survey",
            Status = SurveyStatuses.Draft,
            AuthorId = Guid.NewGuid(),
        };
        var session = new SurveySession
        {
            Id = Guid.NewGuid(),
            SurveyId = survey.Id,
            Survey = survey,
            AccessCode = "DRAFTCOD",
            IsActive = true,
        };
        await db.Surveys.AddAsync(survey);
        await db.SurveySessions.AddAsync(session);
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var result = await service.ClearDraftAsync(UserId, "DRAFTCOD", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAsync_NoDraftParticipant_ReturnsFailure()
    {
        var db = CreateDbContext();
        await SeedPublishedAsync(db);
        var service = BuildService(db);

        var result = await service.SubmitAsync(UserId, ValidCode, SurveyId, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("No draft found.");
    }

    [Fact]
    public async Task SubmitAsync_ParticipantExistsButNoDraft_ReturnsFailure()
    {
        var db = CreateDbContext();
        var (_, session, _) = await SeedPublishedAsync(db);

        var participant = new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            UserId = UserId,
            JoinedAt = DateTime.UtcNow,
        };
        await db.SessionParticipants.AddAsync(participant);
        await db.SaveChangesAsync();

        var service = BuildService(db);
        var result = await service.SubmitAsync(UserId, ValidCode, SurveyId, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("No draft to submit.");
    }

    [Fact]
    public async Task SubmitAsync_DraftWithUnansweredQuestions_ReturnsFailureWithCount()
    {
        var db = CreateDbContext();

        var q1 = new Question { Id = Guid.NewGuid(), SurveyId = SurveyId, Text = "Q1", Type = "Text", OrderNumber = 1 };
        var q2 = new Question { Id = Guid.NewGuid(), SurveyId = SurveyId, Text = "Q2", Type = "Text", OrderNumber = 2 };

        var survey = new Survey
        {
            Id = SurveyId,
            Title = "Two Questions",
            Status = SurveyStatuses.Published,
            IsPublic = true,
            AuthorId = Guid.NewGuid(),
            Questions = new List<Question> { q1, q2 },
        };
        var session = new SurveySession
        {
            Id = Guid.NewGuid(),
            SurveyId = survey.Id,
            Survey = survey,
            AccessCode = ValidCode,
            IsActive = true,
        };
        await db.Surveys.AddAsync(survey);
        await db.SurveySessions.AddAsync(session);
        await db.SaveChangesAsync();

        var service = BuildService(db);

        await service.SaveDraftAsync(UserId, new SaveDraftRequestDto
        {
            SurveyId = SurveyId,
            AccessCode = ValidCode,
            Answers = new List<ParticipationAnswerDto>
            {
                new() { QuestionId = q1.Id, TextAnswer = "Answer1" },
            },
        }, CancellationToken.None);

        var result = await service.SubmitAsync(UserId, ValidCode, SurveyId, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("1 question(s) remaining");
    }

    [Fact]
    public async Task SubmitAsync_AllQuestionsAnswered_ReturnsSuccess()
    {
        var db = CreateDbContext();
        var (survey, _, question) = await SeedPublishedAsync(db);
        var service = BuildService(db);

        await service.SaveDraftAsync(UserId, new SaveDraftRequestDto
        {
            SurveyId = survey.Id,
            AccessCode = ValidCode,
            Answers = new List<ParticipationAnswerDto>
            {
                new() { QuestionId = question.Id, TextAnswer = "My answer" },
            },
        }, CancellationToken.None);

        var result = await service.SubmitAsync(UserId, ValidCode, survey.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAsync_AllQuestionsAnswered_MarksResponseAsNotDraft()
    {
        var db = CreateDbContext();
        var (survey, session, question) = await SeedPublishedAsync(db);
        var service = BuildService(db);

        await service.SaveDraftAsync(UserId, new SaveDraftRequestDto
        {
            SurveyId = survey.Id,
            AccessCode = ValidCode,
            Answers = new List<ParticipationAnswerDto>
            {
                new() { QuestionId = question.Id, TextAnswer = "Final answer" },
            },
        }, CancellationToken.None);

        await service.SubmitAsync(UserId, ValidCode, survey.Id, CancellationToken.None);

        var submitted = await db.Responses
            .Where(r => r.SessionParticipant.SessionId == session.Id
                && r.SessionParticipant.UserId == UserId
                && !r.IsDraft)
            .FirstOrDefaultAsync();

        submitted.Should().NotBeNull();
        submitted!.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitAsync_SurveyMismatch_ReturnsFailure()
    {
        var db = CreateDbContext();
        await SeedPublishedAsync(db);
        var service = BuildService(db);

        var result = await service.SubmitAsync(UserId, ValidCode, Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Survey mismatch.");
    }

    [Fact]
    public async Task GetByCodeAsync_SubmittedSurvey_IsSubmittedIsTrue()
    {
        var db = CreateDbContext();
        var (survey, _, question) = await SeedPublishedAsync(db);
        var service = BuildService(db);

        await service.SaveDraftAsync(UserId, new SaveDraftRequestDto
        {
            SurveyId = survey.Id,
            AccessCode = ValidCode,
            Answers = new List<ParticipationAnswerDto>
            {
                new() { QuestionId = question.Id, TextAnswer = "Done" },
            },
        }, CancellationToken.None);

        await service.SubmitAsync(UserId, ValidCode, survey.Id, CancellationToken.None);

        var result = await service.GetByCodeAsync(ValidCode, UserId, CancellationToken.None);

        result.Value!.IsSubmitted.Should().BeTrue();
    }
}