// <copyright file="ChartServiceTests.cs" company="PlaceholderCompany">
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
using SurveyPro.Application.Interfaces;
using SurveyPro.Domain.Entities;
using SurveyPro.Domain.Enums;
using SurveyPro.Infrastructure.Interfaces;
using SurveyPro.Infrastructure.Persistence;
using SurveyPro.Infrastructure.Services;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ChartService"/>.
/// </summary>
public class ChartServiceTests
{
    private static SurveyProDbContext CreateDbContext() =>
        new SurveyProDbContext(
            new DbContextOptionsBuilder<SurveyProDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    private static ChartService BuildService(
        SurveyProDbContext? dbContext,
        Mock<ISurveyRepository> repo)
    {
        var logger = new Mock<ILogger<ChartService>>();
        return new ChartService(dbContext, repo.Object, logger.Object);
    }

    private static Mock<ISurveyRepository> RepoReturning(Survey? survey)
    {
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        return repo;
    }

    private static Survey MakeSurvey(Guid surveyId, Guid authorId, string title = "Test Survey") =>
        new Survey
        {
            Id = surveyId,
            AuthorId = authorId,
            Title = title,
            Status = SurveyStatuses.Published,
        };

    /// <summary>
    /// Seeds a published survey with one text question and one single-choice question,
    /// plus a submitted response that answers both.
    /// </summary>
    private static async Task<(Survey survey, Question textQ, Question choiceQ, AnswerOption optA, AnswerOption optB)>
        SeedSurveyWithResponseAsync(SurveyProDbContext db, Guid surveyId, Guid authorId)
    {
        var textQ = new Question
        {
            Id = Guid.NewGuid(),
            SurveyId = surveyId,
            Text = "Favourite colour?",
            Type = "Text",
            OrderNumber = 1,
        };

        var optA = new AnswerOption { Id = Guid.NewGuid(), QuestionId = Guid.Empty /* set below */, Text = "Yes" };
        var optB = new AnswerOption { Id = Guid.NewGuid(), QuestionId = Guid.Empty, Text = "No" };

        var choiceQ = new Question
        {
            Id = Guid.NewGuid(),
            SurveyId = surveyId,
            Text = "Do you agree?",
            Type = "SingleChoice",
            OrderNumber = 2,
            Options = new List<AnswerOption> { optA, optB },
        };
        optA.QuestionId = choiceQ.Id;
        optB.QuestionId = choiceQ.Id;

        var survey = new Survey
        {
            Id = surveyId,
            AuthorId = authorId,
            Title = "Seeded Survey",
            Status = SurveyStatuses.Published,
            Questions = new List<Question> { textQ, choiceQ },
        };

        var user = new ApplicationUser { Id = Guid.NewGuid(), Name = "Participant", Email = "p@test.com" };

        var session = new SurveySession
        {
            Id = Guid.NewGuid(),
            SurveyId = survey.Id,
            AccessCode = "TESTCODE",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

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
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Answers = new List<ResponseAnswer>
            {
                new ResponseAnswer
                {
                    Id = Guid.NewGuid(),
                    QuestionId = textQ.Id,
                    TextAnswer = "Blue",
                },
                new ResponseAnswer
                {
                    Id = Guid.NewGuid(),
                    QuestionId = choiceQ.Id,
                    OptionId = optA.Id,
                    Option = optA,
                },
            },
        };

        await db.Users.AddAsync(user);
        await db.Surveys.AddAsync(survey);
        await db.SurveySessions.AddAsync(session);
        await db.SessionParticipants.AddAsync(participant);
        await db.Responses.AddAsync(response);
        await db.SaveChangesAsync();

        return (survey, textQ, choiceQ, optA, optB);
    }

    [Fact]
    public async Task GetSurveyChartsAsync_EmptySurveyId_ReturnsFailure()
    {
        var db = CreateDbContext();
        var service = BuildService(db, RepoReturning(null));

        var result = await service.GetSurveyChartsAsync(
            Guid.Empty, Guid.NewGuid(), isAdministrator: false, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid survey id.");
    }

    [Fact]
    public async Task GetSurveyChartsAsync_EmptyUserId_ReturnsFailure()
    {
        var db = CreateDbContext();
        var service = BuildService(db, RepoReturning(null));

        var result = await service.GetSurveyChartsAsync(
            Guid.NewGuid(), Guid.Empty, isAdministrator: false, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid user id.");
    }

    [Fact]
    public async Task GetSurveyChartsAsync_NullDbContext_ReturnsFailure()
    {
        var service = BuildService(dbContext: null, RepoReturning(null));

        var result = await service.GetSurveyChartsAsync(
            Guid.NewGuid(), Guid.NewGuid(), isAdministrator: false, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("unavailable");
    }

    [Fact]
    public async Task GetSurveyChartsAsync_SurveyNotFound_ReturnsFailure()
    {
        var db = CreateDbContext();
        var service = BuildService(db, RepoReturning(null));

        var result = await service.GetSurveyChartsAsync(
            Guid.NewGuid(), Guid.NewGuid(), isAdministrator: false, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Survey not found.");
    }

    [Fact]
    public async Task GetSurveyChartsAsync_NonAuthorNonAdmin_ReturnsAccessDenied()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var survey = MakeSurvey(surveyId, authorId);

        var db = CreateDbContext();
        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetSurveyChartsAsync(
            surveyId, requesterId, isAdministrator: false, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied.");
    }

    [Fact]
    public async Task GetSurveyChartsAsync_AdminAccessesForeignSurvey_ReturnsSuccess()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var survey = MakeSurvey(surveyId, authorId);

        var db = CreateDbContext();
        await db.Surveys.AddAsync(survey);
        await db.SaveChangesAsync();

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetSurveyChartsAsync(
            surveyId, adminId, isAdministrator: true, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetSurveyChartsAsync_AuthorAccess_ReturnsSuccess()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var survey = MakeSurvey(surveyId, authorId);

        var db = CreateDbContext();
        await db.Surveys.AddAsync(survey);
        await db.SaveChangesAsync();

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetSurveyChartsAsync(
            surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetSurveyChartsAsync_NoResponses_ReturnsTotalSubmittedResponsesZero()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var survey = MakeSurvey(surveyId, authorId);

        var db = CreateDbContext();
        await db.Surveys.AddAsync(survey);
        await db.SaveChangesAsync();

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetSurveyChartsAsync(
            surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.Value!.TotalSubmittedResponses.Should().Be(0);
    }

    [Fact]
    public async Task GetSurveyChartsAsync_TextQuestion_ChartHasCanBeChartedFalse()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, _, _, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetSurveyChartsAsync(
            surveyId, authorId, isAdministrator: false, CancellationToken.None);

        var textChart = result.Value!.Charts.First(c => c.QuestionType == "Text");
        textChart.CanBeCharted.Should().BeFalse();
    }

    [Fact]
    public async Task GetSurveyChartsAsync_TextQuestion_ChartErrorMessageIsSet()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, _, _, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetSurveyChartsAsync(
            surveyId, authorId, isAdministrator: false, CancellationToken.None);

        var textChart = result.Value!.Charts.First(c => c.QuestionType == "Text");
        textChart.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSurveyChartsAsync_ChoiceQuestion_ChartHasCanBeChartedTrue()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, _, _, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetSurveyChartsAsync(
            surveyId, authorId, isAdministrator: false, CancellationToken.None);

        var choiceChart = result.Value!.Charts.First(c => c.QuestionType == "SingleChoice");
        choiceChart.CanBeCharted.Should().BeTrue();
    }

    [Fact]
    public async Task GetSurveyChartsAsync_ChoiceQuestion_PercentagesSumToHundred()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, _, _, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetSurveyChartsAsync(
            surveyId, authorId, isAdministrator: false, CancellationToken.None);

        var choiceChart = result.Value!.Charts.First(c => c.QuestionType == "SingleChoice");
        choiceChart.Labels.Sum(p => p.Percentage).Should().Be(100);
    }

    [Fact]
    public async Task GetSurveyChartsAsync_WithOneResponse_TotalSubmittedResponsesIsOne()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, _, _, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetSurveyChartsAsync(
            surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.Value!.TotalSubmittedResponses.Should().Be(1);
    }

    [Fact]
    public async Task GetSurveyChartsAsync_ChartsCountMatchesQuestionCount()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, _, _, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetSurveyChartsAsync(
            surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.Value!.Charts.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSurveyChartsAsync_HistogramsCountMatchesQuestionCount()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, _, _, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetSurveyChartsAsync(
            surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.Value!.Histograms.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSurveyChartsAsync_DraftResponsesAreExcluded()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();

        var textQ = new Question
        {
            Id = Guid.NewGuid(),
            SurveyId = surveyId,
            Text = "Q?",
            Type = "Text",
            OrderNumber = 1,
        };

        var survey = new Survey
        {
            Id = surveyId,
            AuthorId = authorId,
            Title = "Draft Only Survey",
            Status = SurveyStatuses.Published,
            Questions = new List<Question> { textQ },
        };

        var user = new ApplicationUser { Id = Guid.NewGuid(), Name = "U", Email = "u@u.com" };
        var session = new SurveySession
        {
            Id = Guid.NewGuid(),
            SurveyId = survey.Id,
            AccessCode = "CODE0001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
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
                new ResponseAnswer { Id = Guid.NewGuid(), QuestionId = textQ.Id, TextAnswer = "Draft answer" },
            },
        };

        await db.Users.AddAsync(user);
        await db.Surveys.AddAsync(survey);
        await db.SurveySessions.AddAsync(session);
        await db.SessionParticipants.AddAsync(participant);
        await db.Responses.AddAsync(draft);
        await db.SaveChangesAsync();

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetSurveyChartsAsync(
            surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.Value!.TotalSubmittedResponses.Should().Be(0);
    }

    [Fact]
    public async Task GetSurveyChartsAsync_SurveyTitleIsMappedCorrectly()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var survey = MakeSurvey(surveyId, authorId, title: "My Special Survey");

        var db = CreateDbContext();
        await db.Surveys.AddAsync(survey);
        await db.SaveChangesAsync();

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetSurveyChartsAsync(
            surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.Value!.SurveyTitle.Should().Be("My Special Survey");
    }

    [Fact]
    public async Task GetSurveyChartsAsync_TextHistogram_BucketsContainTextAnswers()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, textQ, _, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetSurveyChartsAsync(
            surveyId, authorId, isAdministrator: false, CancellationToken.None);

        var textHistogram = result.Value!.Histograms
            .First(h => h.QuestionId == textQ.Id.ToString());

        textHistogram.Buckets.Should().ContainSingle(b => b.Label == "Blue");
    }

    [Fact]
    public async Task GetSurveyChartsAsync_TextHistogram_TotalResponsesIsCorrect()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, textQ, _, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetSurveyChartsAsync(
            surveyId, authorId, isAdministrator: false, CancellationToken.None);

        var textHistogram = result.Value!.Histograms
            .First(h => h.QuestionId == textQ.Id.ToString());

        textHistogram.TotalResponses.Should().Be(1);
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_EmptyQuestionId_ReturnsFailure()
    {
        var db = CreateDbContext();
        var service = BuildService(db, RepoReturning(null));

        var result = await service.GetQuestionHistogramAsync(
            Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), isAdministrator: false, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid question id.");
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_EmptySurveyId_ReturnsFailure()
    {
        var db = CreateDbContext();
        var service = BuildService(db, RepoReturning(null));

        var result = await service.GetQuestionHistogramAsync(
            Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), isAdministrator: false, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid survey id.");
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_NullDbContext_ReturnsFailure()
    {
        var service = BuildService(dbContext: null, RepoReturning(null));

        var result = await service.GetQuestionHistogramAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), isAdministrator: false, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("unavailable");
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_SurveyNotFound_ReturnsFailure()
    {
        var db = CreateDbContext();
        var service = BuildService(db, RepoReturning(null));

        var result = await service.GetQuestionHistogramAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), isAdministrator: false, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Survey not found.");
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_NonAuthorNonAdmin_ReturnsAccessDenied()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var survey = MakeSurvey(surveyId, authorId);

        var db = CreateDbContext();
        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetQuestionHistogramAsync(
            Guid.NewGuid(), surveyId, requesterId, isAdministrator: false, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied.");
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_QuestionNotFound_ReturnsFailure()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var survey = MakeSurvey(surveyId, authorId);

        var db = CreateDbContext();
        await db.Surveys.AddAsync(survey);
        await db.SaveChangesAsync();

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetQuestionHistogramAsync(
            Guid.NewGuid(), surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Question not found.");
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_TextQuestion_ReturnsSuccess()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, textQ, _, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetQuestionHistogramAsync(
            textQ.Id, surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_TextQuestion_BucketsContainAnswer()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, textQ, _, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetQuestionHistogramAsync(
            textQ.Id, surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.Value!.Buckets.Should().ContainSingle(b => b.Label == "Blue");
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_TextQuestion_TotalResponsesIsOne()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, textQ, _, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetQuestionHistogramAsync(
            textQ.Id, surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.Value!.TotalResponses.Should().Be(1);
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_ChoiceQuestion_ReturnsSuccess()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, _, choiceQ, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetQuestionHistogramAsync(
            choiceQ.Id, surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_ChoiceQuestion_BucketsContainSelectedOption()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, _, choiceQ, optA, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetQuestionHistogramAsync(
            choiceQ.Id, surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.Value!.Buckets.Should().ContainSingle(b => b.Label == optA.Text);
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_ChoiceQuestion_PercentageIs100ForSingleAnswer()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, _, choiceQ, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetQuestionHistogramAsync(
            choiceQ.Id, surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.Value!.Buckets.First().Percentage.Should().Be(100);
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_AdminAccessesForeignSurvey_ReturnsSuccess()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, textQ, _, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetQuestionHistogramAsync(
            textQ.Id, surveyId, adminId, isAdministrator: true, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_DraftResponsesExcluded_TotalResponsesIsZero()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();

        var question = new Question
        {
            Id = Guid.NewGuid(),
            SurveyId = surveyId,
            Text = "Q?",
            Type = "Text",
            OrderNumber = 1,
        };

        var survey = new Survey
        {
            Id = surveyId,
            AuthorId = authorId,
            Title = "Survey",
            Status = SurveyStatuses.Published,
            Questions = new List<Question> { question },
        };

        var user = new ApplicationUser { Id = Guid.NewGuid(), Name = "U", Email = "u@u.com" };
        var session = new SurveySession
        {
            Id = Guid.NewGuid(),
            SurveyId = survey.Id,
            AccessCode = "DRAFTONLY",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var participant = new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            UserId = user.Id,
            JoinedAt = DateTime.UtcNow,
        };
        var draftResponse = new Response
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
                    TextAnswer = "Should not appear",
                },
            },
        };

        await db.Users.AddAsync(user);
        await db.Surveys.AddAsync(survey);
        await db.SurveySessions.AddAsync(session);
        await db.SessionParticipants.AddAsync(participant);
        await db.Responses.AddAsync(draftResponse);
        await db.SaveChangesAsync();

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetQuestionHistogramAsync(
            question.Id, surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.Value!.TotalResponses.Should().Be(0);
        result.Value.Buckets.Should().BeEmpty();
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_ChoiceQuestion_QuestionIdMappedCorrectly()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, _, choiceQ, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetQuestionHistogramAsync(
            choiceQ.Id, surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.Value!.QuestionId.Should().Be(choiceQ.Id.ToString());
    }

    [Fact]
    public async Task GetQuestionHistogramAsync_TextQuestion_CanBeChartedIsTrueWhenAnswersExist()
    {
        var surveyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var db = CreateDbContext();
        var (survey, textQ, _, _, _) = await SeedSurveyWithResponseAsync(db, surveyId, authorId);

        var service = BuildService(db, RepoReturning(survey));

        var result = await service.GetQuestionHistogramAsync(
            textQ.Id, surveyId, authorId, isAdministrator: false, CancellationToken.None);

        result.Value!.CanBeCharted.Should().BeTrue();
    }
}