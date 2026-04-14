// <copyright file="SurveyResponsesControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyPro.Application.Common;
using SurveyPro.Application.DTOs.Surveys;
using SurveyPro.Application.Interfaces;
using SurveyPro.Web.Controllers;
using SurveyPro.Web.ViewModels.Surveys;
using Xunit;

/// <summary>
/// Unit tests for survey responses viewing and export actions
/// in <see cref="SurveysController"/>.
/// </summary>
public class SurveyResponsesControllerTests
{
    private static readonly Guid ValidAuthorId = Guid.NewGuid();
    private static readonly Guid ValidSurveyId = Guid.NewGuid();

    private static SurveysController BuildController(
        Mock<ISurveyService> surveyService,
        Guid? userId = null,
        bool isAdmin = false)
    {
        var logger = new Mock<ILogger<SurveysController>>();
        var questionService = new Mock<IQuestionService>();

        var controller = new SurveysController(
            surveyService.Object,
            logger.Object,
            questionService.Object);

        var actualUserId = userId ?? ValidAuthorId;
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, actualUserId.ToString()),
            new Claim(ClaimTypes.Role, "Author"),
        };

        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return controller;
    }

    private static SurveysController BuildControllerWithoutUser(Mock<ISurveyService> surveyService)
    {
        var logger = new Mock<ILogger<SurveysController>>();
        var questionService = new Mock<IQuestionService>();

        var controller = new SurveysController(
            surveyService.Object,
            logger.Object,
            questionService.Object);

        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return controller;
    }

    private static SurveyResponsesDto MakeResponsesDto(int responseCount = 1) => new SurveyResponsesDto
    {
        SurveyId = ValidSurveyId,
        SurveyTitle = "Customer Feedback",
        SurveyDescription = "Annual feedback survey",
        AccessCode = "TESTCODE",
        TotalSubmittedResponses = responseCount,
        Responses = Enumerable.Range(0, responseCount).Select(i => new SurveyResponseDto
        {
            ResponseId = Guid.NewGuid(),
            RespondentUserId = Guid.NewGuid(),
            RespondentName = $"User {i + 1}",
            RespondentEmail = $"user{i + 1}@example.com",
            SubmittedAt = DateTime.UtcNow.AddMinutes(-i),
            Answers = new[]
            {
                new SurveyResponseAnswerDto
                {
                    QuestionId = Guid.NewGuid(),
                    QuestionOrderNumber = 1,
                    QuestionText = "How satisfied are you?",
                    QuestionType = "SingleChoice",
                    SelectedOptionTexts = new[] { "Very satisfied" },
                    SelectedOptionIds = new[] { Guid.NewGuid() },
                },
                new SurveyResponseAnswerDto
                {
                    QuestionId = Guid.NewGuid(),
                    QuestionOrderNumber = 2,
                    QuestionText = "Any comments?",
                    QuestionType = "Text",
                    TextAnswer = $"Comment from user {i + 1}",
                },
            },
        }).ToList(),
    };

    [Fact]
    public async Task Responses_NoAuthenticatedUser_RedirectsToLogin()
    {
        var service = new Mock<ISurveyService>();
        var controller = BuildControllerWithoutUser(service);

        var result = await controller.Responses(ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task Responses_ServiceFailure_SetsTempDataErrorAndRedirectsToMy()
    {
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Failure("Survey not found."));

        var controller = BuildController(service);

        var result = await controller.Responses(ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("My");
        controller.TempData["ErrorMessage"].Should().Be("Survey not found.");
    }

    [Fact]
    public async Task Responses_AccessDenied_SetsTempDataErrorAndRedirectsToMy()
    {
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Failure("Access denied."));

        var controller = BuildController(service);

        var result = await controller.Responses(ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("My");
        controller.TempData["ErrorMessage"].Should().Be("Access denied.");
    }

    [Fact]
    public async Task Responses_Success_ReturnsViewWithViewModel()
    {
        var dto = MakeResponsesDto();
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(dto));

        var controller = BuildController(service);

        var result = await controller.Responses(ValidSurveyId, CancellationToken.None);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeOfType<SurveyResponsesViewModel>();
    }

    [Fact]
    public async Task Responses_Success_ViewModelHasCorrectSurveyMetadata()
    {
        var dto = MakeResponsesDto();
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(dto));

        var controller = BuildController(service);

        var result = await controller.Responses(ValidSurveyId, CancellationToken.None);

        var model = result.Should().BeOfType<ViewResult>().Subject
            .Model.Should().BeOfType<SurveyResponsesViewModel>().Subject;

        model.SurveyId.Should().Be(ValidSurveyId);
        model.SurveyTitle.Should().Be("Customer Feedback");
        model.SurveyDescription.Should().Be("Annual feedback survey");
        model.AccessCode.Should().Be("TESTCODE");
    }

    [Fact]
    public async Task Responses_Success_ViewModelHasCorrectResponseCount()
    {
        var dto = MakeResponsesDto(responseCount: 3);
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(dto));

        var controller = BuildController(service);

        var result = await controller.Responses(ValidSurveyId, CancellationToken.None);

        var model = result.Should().BeOfType<ViewResult>().Subject
            .Model.Should().BeOfType<SurveyResponsesViewModel>().Subject;

        model.TotalSubmittedResponses.Should().Be(3);
        model.Responses.Should().HaveCount(3);
    }

    [Fact]
    public async Task Responses_Success_ResponsesAreOrderedBySubmittedAtDescending()
    {
        var dto = MakeResponsesDto(responseCount: 3);
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(dto));

        var controller = BuildController(service);

        var result = await controller.Responses(ValidSurveyId, CancellationToken.None);

        var model = result.Should().BeOfType<ViewResult>().Subject
            .Model.Should().BeOfType<SurveyResponsesViewModel>().Subject;

        model.Responses.Should().BeInDescendingOrder(r => r.SubmittedAt);
    }

    [Fact]
    public async Task Responses_Success_AnswersAreOrderedByQuestionOrderNumber()
    {
        var dto = MakeResponsesDto();
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(dto));

        var controller = BuildController(service);

        var result = await controller.Responses(ValidSurveyId, CancellationToken.None);

        var model = result.Should().BeOfType<ViewResult>().Subject
            .Model.Should().BeOfType<SurveyResponsesViewModel>().Subject;

        model.Responses[0].Answers
            .Should().BeInAscendingOrder(a => a.QuestionOrderNumber);
    }

    [Fact]
    public async Task Responses_Success_RespondentDataIsMappedCorrectly()
    {
        var dto = MakeResponsesDto();
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(dto));

        var controller = BuildController(service);

        var result = await controller.Responses(ValidSurveyId, CancellationToken.None);

        var model = result.Should().BeOfType<ViewResult>().Subject
            .Model.Should().BeOfType<SurveyResponsesViewModel>().Subject;

        var response = model.Responses[0];
        response.RespondentName.Should().Be("User 1");
        response.RespondentEmail.Should().Be("user1@example.com");
    }

    [Fact]
    public async Task Responses_AsAdmin_PassesIsAdministratorTrueToService()
    {
        bool? capturedIsAdmin = null;
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(
                ValidSurveyId, ValidAuthorId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, bool, CancellationToken>((_, _, isAdmin, _) => capturedIsAdmin = isAdmin)
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(MakeResponsesDto()));

        var controller = BuildController(service, isAdmin: true);

        await controller.Responses(ValidSurveyId, CancellationToken.None);

        capturedIsAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task Responses_AsAuthorNotAdmin_PassesIsAdministratorFalseToService()
    {
        bool? capturedIsAdmin = null;
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(
                ValidSurveyId, ValidAuthorId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, bool, CancellationToken>((_, _, isAdmin, _) => capturedIsAdmin = isAdmin)
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(MakeResponsesDto()));

        var controller = BuildController(service, isAdmin: false);

        await controller.Responses(ValidSurveyId, CancellationToken.None);

        capturedIsAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task Responses_EmptyResponses_ReturnsViewWithZeroCount()
    {
        var dto = new SurveyResponsesDto
        {
            SurveyId = ValidSurveyId,
            SurveyTitle = "Empty Survey",
            TotalSubmittedResponses = 0,
            Responses = Array.Empty<SurveyResponseDto>(),
        };

        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(dto));

        var controller = BuildController(service);

        var result = await controller.Responses(ValidSurveyId, CancellationToken.None);

        var model = result.Should().BeOfType<ViewResult>().Subject
            .Model.Should().BeOfType<SurveyResponsesViewModel>().Subject;

        model.TotalSubmittedResponses.Should().Be(0);
        model.Responses.Should().BeEmpty();
    }

    [Fact]
    public async Task Responses_CallsServiceWithCorrectSurveyId()
    {
        Guid capturedSurveyId = Guid.Empty;
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, bool, CancellationToken>((sid, _, _, _) => capturedSurveyId = sid)
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(MakeResponsesDto()));

        var controller = BuildController(service);

        await controller.Responses(ValidSurveyId, CancellationToken.None);

        capturedSurveyId.Should().Be(ValidSurveyId);
    }

    [Fact]
    public async Task ExportResponsesCsv_NoAuthenticatedUser_RedirectsToLogin()
    {
        var service = new Mock<ISurveyService>();
        var controller = BuildControllerWithoutUser(service);

        var result = await controller.ExportResponsesCsv(ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task ExportResponsesCsv_ServiceFailure_SetsTempDataErrorAndRedirectsToResponses()
    {
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Failure("Access denied."));

        var controller = BuildController(service);

        var result = await controller.ExportResponsesCsv(ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Responses");
        controller.TempData["ErrorMessage"].Should().Be("Access denied.");
    }

    [Fact]
    public async Task ExportResponsesCsv_Success_ReturnsFileResult()
    {
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(MakeResponsesDto()));

        var controller = BuildController(service);

        var result = await controller.ExportResponsesCsv(ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<FileContentResult>();
    }

    [Fact]
    public async Task ExportResponsesCsv_Success_ContentTypeIsTextCsv()
    {
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(MakeResponsesDto()));

        var controller = BuildController(service);

        var result = await controller.ExportResponsesCsv(ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<FileContentResult>()
            .Which.ContentType.Should().Be("text/csv");
    }

    [Fact]
    public async Task ExportResponsesCsv_Success_FileNameContainsSurveyTitle()
    {
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(MakeResponsesDto()));

        var controller = BuildController(service);

        var result = await controller.ExportResponsesCsv(ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<FileContentResult>()
            .Which.FileDownloadName.Should().Contain("Customer Feedback");
    }

    [Fact]
    public async Task ExportResponsesCsv_Success_FileContentIsNotEmpty()
    {
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(MakeResponsesDto()));

        var controller = BuildController(service);

        var result = await controller.ExportResponsesCsv(ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<FileContentResult>()
            .Which.FileContents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportResponsesPdf_NoAuthenticatedUser_RedirectsToLogin()
    {
        var service = new Mock<ISurveyService>();
        var controller = BuildControllerWithoutUser(service);

        var result = await controller.ExportResponsesPdf(ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task ExportResponsesPdf_ServiceFailure_SetsTempDataErrorAndRedirectsToResponses()
    {
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Failure("Survey not found."));

        var controller = BuildController(service);

        var result = await controller.ExportResponsesPdf(ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Responses");
        controller.TempData["ErrorMessage"].Should().Be("Survey not found.");
    }

    [Fact]
    public async Task ExportResponsesPdf_Success_ReturnsFileResult()
    {
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(MakeResponsesDto()));

        var controller = BuildController(service);

        var result = await controller.ExportResponsesPdf(ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<FileContentResult>();
    }

    [Fact]
    public async Task ExportResponsesPdf_Success_ContentTypeIsApplicationPdf()
    {
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(MakeResponsesDto()));

        var controller = BuildController(service);

        var result = await controller.ExportResponsesPdf(ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<FileContentResult>()
            .Which.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task ExportResponsesPdf_Success_FileNameContainsSurveyTitle()
    {
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(MakeResponsesDto()));

        var controller = BuildController(service);

        var result = await controller.ExportResponsesPdf(ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<FileContentResult>()
            .Which.FileDownloadName.Should().Contain("Customer Feedback");
    }

    [Fact]
    public async Task ExportResponsesPdf_Success_FileContentIsNotEmpty()
    {
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetSurveyResponsesAsync(ValidSurveyId, ValidAuthorId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyResponsesDto>.Success(MakeResponsesDto()));

        var controller = BuildController(service);

        var result = await controller.ExportResponsesPdf(ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<FileContentResult>()
            .Which.FileContents.Should().NotBeEmpty();
    }
}