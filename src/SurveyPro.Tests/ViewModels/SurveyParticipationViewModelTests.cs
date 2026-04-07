// <copyright file="SurveyParticipationViewModelTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Tests.Participation;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using SurveyPro.Application.Common;
using SurveyPro.Application.DTOs.Participation;
using SurveyPro.Application.Interfaces;
using SurveyPro.Web.Controllers;
using SurveyPro.Web.ViewModels.Participation;
using Xunit;

/// <summary>
/// Unit tests for <see cref="SurveyParticipationViewModel"/> and view model mapping.
/// </summary>
public class SurveyParticipationViewModelTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private static readonly Guid ValidSurveyId = Guid.NewGuid();
    private const string ValidCode = "ABCD1234";

    private static ParticipationController BuildController(
        Mock<ISurveyParticipationService> service,
        Guid? userId = null)
    {
        var controller = new ParticipationController(service.Object);

        var actualUserId = userId ?? ValidUserId;
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, actualUserId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return controller;
    }

    private static SurveyParticipationDto MakeSurveyDto(bool isSubmitted = false) => new()
    {
        SurveyId = ValidSurveyId,
        AccessCode = ValidCode,
        Title = "Test Survey",
        IsPublic = true,
        IsSubmitted = isSubmitted,
        Questions = new List<ParticipationQuestionDto>
        {
            new()
            {
                QuestionId = Guid.NewGuid(),
                Text = "Favourite colour?",
                Type = "Text",
                OrderNumber = 1,
                Options = Array.Empty<ParticipationOptionDto>(),
            },
        },
        DraftAnswers = Array.Empty<ParticipationAnswerDto>(),
    };

    [Fact]
    public async Task Join_Get_DraftAnswersArePrefilledInViewModel()
    {
        var questionId = Guid.NewGuid();
        var dto = new SurveyParticipationDto
        {
            SurveyId = ValidSurveyId,
            AccessCode = ValidCode,
            Title = "T",
            IsPublic = true,
            Questions = new List<ParticipationQuestionDto>
            {
                new()
                {
                    QuestionId = questionId,
                    Text = "Q",
                    Type = "Text",
                    OrderNumber = 1,
                    Options = Array.Empty<ParticipationOptionDto>(),
                },
            },
            DraftAnswers = new List<ParticipationAnswerDto>
            {
                new() { QuestionId = questionId, TextAnswer = "My answer" },
            },
        };

        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.GetByCodeAsync(ValidCode, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyParticipationDto>.Success(dto));

        var controller = BuildController(service);

        var result = await controller.Join(ValidCode, CancellationToken.None);

        var model = result.Should().BeOfType<ViewResult>().Subject
            .Model.Should().BeOfType<SurveyParticipationViewModel>().Subject;

        model.Questions[0].TextAnswer.Should().Be("My answer");
    }

    [Fact]
    public async Task Join_Get_SingleChoiceDraftAnswerIsPrefilledInViewModel()
    {
        var questionId = Guid.NewGuid();
        var optionId = Guid.NewGuid();

        var dto = new SurveyParticipationDto
        {
            SurveyId = ValidSurveyId,
            AccessCode = ValidCode,
            Title = "T",
            IsPublic = true,
            Questions = new List<ParticipationQuestionDto>
            {
                new()
                {
                    QuestionId = questionId,
                    Text = "Q",
                    Type = "SingleChoice",
                    OrderNumber = 1,
                    Options = new List<ParticipationOptionDto>
                    {
                        new() { Id = optionId, Text = "Option A" },
                    },
                },
            },
            DraftAnswers = new List<ParticipationAnswerDto>
            {
                new() { QuestionId = questionId, SelectedOptionIds = new List<Guid> { optionId } },
            },
        };

        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.GetByCodeAsync(ValidCode, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyParticipationDto>.Success(dto));

        var controller = BuildController(service);

        var result = await controller.Join(ValidCode, CancellationToken.None);

        var model = result.Should().BeOfType<ViewResult>().Subject
            .Model.Should().BeOfType<SurveyParticipationViewModel>().Subject;

        model.Questions[0].SelectedOptionId.Should().Be(optionId);
    }

    [Fact]
    public async Task Join_Get_IsSubmittedFlagIsPreservedInViewModel()
    {
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.GetByCodeAsync(ValidCode, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyParticipationDto>.Success(MakeSurveyDto(isSubmitted: true)));

        var controller = BuildController(service);

        var result = await controller.Join(ValidCode, CancellationToken.None);

        var model = result.Should().BeOfType<ViewResult>().Subject
            .Model.Should().BeOfType<SurveyParticipationViewModel>().Subject;

        model.IsSubmitted.Should().BeTrue();
    }

    [Fact]
    public async Task Join_Get_SurveyMetadataIsMappedCorrectly()
    {
        var dto = new SurveyParticipationDto
        {
            SurveyId = ValidSurveyId,
            AccessCode = ValidCode,
            Title = "Test Survey Title",
            Description = "Test Survey Description",
            IsPublic = true,
            Questions = new List<ParticipationQuestionDto>(),
            DraftAnswers = new List<ParticipationAnswerDto>(),
        };

        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.GetByCodeAsync(ValidCode, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyParticipationDto>.Success(dto));

        var controller = BuildController(service);

        var result = await controller.Join(ValidCode, CancellationToken.None);

        var model = result.Should().BeOfType<ViewResult>().Subject
            .Model.Should().BeOfType<SurveyParticipationViewModel>().Subject;

        model.Title.Should().Be("Test Survey Title");
        model.Description.Should().Be("Test Survey Description");
        model.AccessCode.Should().Be(ValidCode);
        model.SurveyId.Should().Be(ValidSurveyId);
        model.IsPublic.Should().BeTrue();
    }

    [Fact]
    public async Task Join_Get_QuestionsAreOrderedByOrderNumber()
    {
        var q1Id = Guid.NewGuid();
        var q2Id = Guid.NewGuid();
        var q3Id = Guid.NewGuid();

        var dto = new SurveyParticipationDto
        {
            SurveyId = ValidSurveyId,
            AccessCode = ValidCode,
            Title = "T",
            IsPublic = true,
            Questions = new List<ParticipationQuestionDto>
            {
                new() { QuestionId = q1Id, Text = "Q1", Type = "Text", OrderNumber = 3, Options = new List<ParticipationOptionDto>() },
                new() { QuestionId = q2Id, Text = "Q2", Type = "Text", OrderNumber = 1, Options = new List<ParticipationOptionDto>() },
                new() { QuestionId = q3Id, Text = "Q3", Type = "Text", OrderNumber = 2, Options = new List<ParticipationOptionDto>() },
            },
            DraftAnswers = new List<ParticipationAnswerDto>(),
        };

        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.GetByCodeAsync(ValidCode, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyParticipationDto>.Success(dto));

        var controller = BuildController(service);

        var result = await controller.Join(ValidCode, CancellationToken.None);

        var model = result.Should().BeOfType<ViewResult>().Subject
            .Model.Should().BeOfType<SurveyParticipationViewModel>().Subject;

        model.Questions.Should().HaveCount(3);
        model.Questions[0].OrderNumber.Should().Be(1);
        model.Questions[1].OrderNumber.Should().Be(2);
        model.Questions[2].OrderNumber.Should().Be(3);
    }

    [Fact]
    public async Task Join_Get_MultipleChoiceOptionsAreMappedCorrectly()
    {
        var questionId = Guid.NewGuid();
        var option1Id = Guid.NewGuid();
        var option2Id = Guid.NewGuid();

        var dto = new SurveyParticipationDto
        {
            SurveyId = ValidSurveyId,
            AccessCode = ValidCode,
            Title = "T",
            IsPublic = true,
            Questions = new List<ParticipationQuestionDto>
            {
                new()
                {
                    QuestionId = questionId,
                    Text = "Q",
                    Type = "MultipleChoice",
                    OrderNumber = 1,
                    Options = new List<ParticipationOptionDto>
                    {
                        new() { Id = option1Id, Text = "Option 1" },
                        new() { Id = option2Id, Text = "Option 2" },
                    },
                },
            },
            DraftAnswers = new List<ParticipationAnswerDto>(),
        };

        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.GetByCodeAsync(ValidCode, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyParticipationDto>.Success(dto));

        var controller = BuildController(service);

        var result = await controller.Join(ValidCode, CancellationToken.None);

        var model = result.Should().BeOfType<ViewResult>().Subject
            .Model.Should().BeOfType<SurveyParticipationViewModel>().Subject;

        model.Questions[0].Options.Should().HaveCount(2);
        model.Questions[0].Options[0].Id.Should().Be(option1Id);
        model.Questions[0].Options[0].Text.Should().Be("Option 1");
        model.Questions[0].Options[1].Id.Should().Be(option2Id);
        model.Questions[0].Options[1].Text.Should().Be("Option 2");
    }
}
