// <copyright file="ParticipationControllerTests.cs" company="PlaceholderCompany">
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
/// Unit tests for <see cref="ParticipationController"/>.
/// </summary>
public class ParticipationControllerTests
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

    private static ParticipationController BuildControllerWithoutUser(
        Mock<ISurveyParticipationService> service)
    {
        var controller = new ParticipationController(service.Object);
        var identity = new ClaimsIdentity();
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

    private static SurveyParticipationViewModel MakeViewModel(bool isSubmitted = false)
    {
        var questionId = Guid.NewGuid();
        return new SurveyParticipationViewModel
        {
            SurveyId = ValidSurveyId,
            AccessCode = ValidCode,
            IsSubmitted = isSubmitted,
            Questions = new List<ParticipationQuestionViewModel>
            {
                new()
                {
                    QuestionId = questionId,
                    Type = "Text",
                    OrderNumber = 1,
                    TextAnswer = "Blue",
                },
            },
        };
    }

    [Fact]
    public async Task Join_Get_EmptyCode_RedirectsToSurveys()
    {
        var service = new Mock<ISurveyParticipationService>();
        var controller = BuildController(service);

        var result = await controller.Join(string.Empty, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
        controller.TempData["ErrorMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Join_Get_SurveyNotFound_RedirectsToSurveys()
    {
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.GetByCodeAsync(ValidCode, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyParticipationDto>.Failure("Survey not found."));

        var controller = BuildController(service);

        var result = await controller.Join(ValidCode, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
        controller.TempData["ErrorMessage"].Should().Be("Survey not found.");
    }

    [Fact]
    public async Task Join_Get_ValidCode_ReturnsViewWithModel()
    {
        var dto = MakeSurveyDto();
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.GetByCodeAsync(ValidCode, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyParticipationDto>.Success(dto));

        var controller = BuildController(service);

        var result = await controller.Join(ValidCode, CancellationToken.None);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<SurveyParticipationViewModel>().Subject;
        model.Title.Should().Be("Test Survey");
        model.AccessCode.Should().Be(ValidCode);
    }

    [Fact]
    public async Task Join_Get_AnonymousUser_PassesNullUserIdToService()
    {
        Guid? capturedUserId = Guid.Empty;
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.GetByCodeAsync(ValidCode, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Callback<string, Guid?, CancellationToken>((_, uid, _) => capturedUserId = uid)
            .ReturnsAsync(Result<SurveyParticipationDto>.Success(MakeSurveyDto()));

        var controller = BuildControllerWithoutUser(service);

        await controller.Join(ValidCode, CancellationToken.None);

        capturedUserId.Should().BeNull();
    }

    [Fact]
    public async Task Join_Post_InvalidModel_ReturnsView()
    {
        var service = new Mock<ISurveyParticipationService>();
        var controller = BuildController(service);
        controller.ModelState.AddModelError("x", "error");
        var model = MakeViewModel();

        var result = await controller.Join(model, CancellationToken.None);

        result.Should().BeOfType<ViewResult>().Which.Model.Should().Be(model);
    }

    [Fact]
    public async Task Join_Post_NoAuthenticatedUser_RedirectsToLogin()
    {
        var service = new Mock<ISurveyParticipationService>();
        var controller = BuildControllerWithoutUser(service);

        var result = await controller.Join(MakeViewModel(), CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task Join_Post_ServiceFailure_SetsTempDataErrorAndReturnsView()
    {
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.SaveDraftAsync(ValidUserId, It.IsAny<SaveDraftRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Survey already submitted. You cannot edit your answers."));

        var controller = BuildController(service);

        var result = await controller.Join(MakeViewModel(), CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        controller.TempData["ErrorMessage"].Should()
            .Be("Survey already submitted. You cannot edit your answers.");
    }

    [Fact]
    public async Task Join_Post_Success_SetsTempDataAndRedirectsToJoin()
    {
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.SaveDraftAsync(ValidUserId, It.IsAny<SaveDraftRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);

        var result = await controller.Join(MakeViewModel(), CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Join");
        redirect.RouteValues!["code"].Should().Be(ValidCode);
        controller.TempData["SuccessMessage"].Should().Be("Draft saved.");
    }

    [Fact]
    public async Task Join_Post_TextAnswer_PassesAnswerToService()
    {
        SaveDraftRequestDto? captured = null;
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.SaveDraftAsync(ValidUserId, It.IsAny<SaveDraftRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, SaveDraftRequestDto, CancellationToken>((_, dto, _) => captured = dto)
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);
        var model = MakeViewModel();
        model.Questions[0].TextAnswer = "Blue";

        await controller.Join(model, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Answers.Should().ContainSingle(a => a.TextAnswer == "Blue");
    }

    [Fact]
    public async Task Join_Post_SingleChoiceAnswer_PassesSelectedOptionIdToService()
    {
        var optionId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        SaveDraftRequestDto? captured = null;

        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.SaveDraftAsync(ValidUserId, It.IsAny<SaveDraftRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, SaveDraftRequestDto, CancellationToken>((_, dto, _) => captured = dto)
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);
        var model = new SurveyParticipationViewModel
        {
            SurveyId = ValidSurveyId,
            AccessCode = ValidCode,
            Questions = new List<ParticipationQuestionViewModel>
            {
                new()
                {
                    QuestionId = questionId,
                    Type = "SingleChoice",
                    SelectedOptionId = optionId,
                },
            },
        };

        await controller.Join(model, CancellationToken.None);

        captured!.Answers.Should().ContainSingle(a =>
            a.QuestionId == questionId &&
            a.SelectedOptionIds.Contains(optionId));
    }

    [Fact]
    public async Task Join_Post_MultipleChoiceAnswer_PassesAllSelectedOptionIdsToService()
    {
        var option1 = Guid.NewGuid();
        var option2 = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        SaveDraftRequestDto? captured = null;

        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.SaveDraftAsync(ValidUserId, It.IsAny<SaveDraftRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, SaveDraftRequestDto, CancellationToken>((_, dto, _) => captured = dto)
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);
        var model = new SurveyParticipationViewModel
        {
            SurveyId = ValidSurveyId,
            AccessCode = ValidCode,
            Questions = new List<ParticipationQuestionViewModel>
            {
                new()
                {
                    QuestionId = questionId,
                    Type = "MultipleChoice",
                    SelectedOptionIds = new List<Guid> { option1, option2 },
                },
            },
        };

        await controller.Join(model, CancellationToken.None);

        captured!.Answers.Should().ContainSingle(a => a.QuestionId == questionId);
        captured.Answers.First(a => a.QuestionId == questionId)
            .SelectedOptionIds.Should().Contain(new[] { option1, option2 });
    }

    [Fact]
    public async Task SaveDraft_NoAuthenticatedUser_ReturnsUnauthorized()
    {
        var service = new Mock<ISurveyParticipationService>();
        var controller = BuildControllerWithoutUser(service);

        var result = await controller.SaveDraft(MakeViewModel(), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task SaveDraft_ServiceFailure_ReturnsBadRequest()
    {
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.SaveDraftAsync(ValidUserId, It.IsAny<SaveDraftRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Survey not found."));

        var controller = BuildController(service);

        var result = await controller.SaveDraft(MakeViewModel(), CancellationToken.None);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().Be("Survey not found.");
    }

    [Fact]
    public async Task SaveDraft_Success_ReturnsOk()
    {
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.SaveDraftAsync(ValidUserId, It.IsAny<SaveDraftRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);

        var result = await controller.SaveDraft(MakeViewModel(), CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task SaveDraft_CallsServiceWithCorrectSurveyIdAndCode()
    {
        SaveDraftRequestDto? captured = null;
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.SaveDraftAsync(ValidUserId, It.IsAny<SaveDraftRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, SaveDraftRequestDto, CancellationToken>((_, dto, _) => captured = dto)
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);

        await controller.SaveDraft(MakeViewModel(), CancellationToken.None);

        captured!.SurveyId.Should().Be(ValidSurveyId);
        captured.AccessCode.Should().Be(ValidCode);
    }

    [Fact]
    public async Task SaveDraft_CallsServiceWithCorrectUserId()
    {
        Guid capturedUserId = Guid.Empty;
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.SaveDraftAsync(It.IsAny<Guid>(), It.IsAny<SaveDraftRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, SaveDraftRequestDto, CancellationToken>((uid, _, _) => capturedUserId = uid)
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);

        await controller.SaveDraft(MakeViewModel(), CancellationToken.None);

        capturedUserId.Should().Be(ValidUserId);
    }

    [Fact]
    public async Task Clear_NoAuthenticatedUser_RedirectsToLogin()
    {
        var service = new Mock<ISurveyParticipationService>();
        var controller = BuildControllerWithoutUser(service);

        var result = await controller.Clear(ValidCode, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task Clear_ServiceFailure_SetsTempDataError()
    {
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.ClearDraftAsync(ValidUserId, ValidCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Survey already submitted."));

        var controller = BuildController(service);

        var result = await controller.Clear(ValidCode, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Join");
        controller.TempData["ErrorMessage"].Should().Be("Survey already submitted.");
    }

    [Fact]
    public async Task Clear_Success_SetsTempDataSuccessAndRedirectsToJoin()
    {
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.ClearDraftAsync(ValidUserId, ValidCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);

        var result = await controller.Clear(ValidCode, CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Join");
        redirect.RouteValues!["code"].Should().Be(ValidCode);
        controller.TempData["SuccessMessage"].Should().Be("All answers cleared.");
    }

    [Fact]
    public async Task Clear_CallsServiceWithCorrectUserIdAndCode()
    {
        Guid capturedUserId = Guid.Empty;
        string? capturedCode = null;

        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.ClearDraftAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, CancellationToken>((uid, code, _) =>
            {
                capturedUserId = uid;
                capturedCode = code;
            })
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);

        await controller.Clear(ValidCode, CancellationToken.None);

        capturedUserId.Should().Be(ValidUserId);
        capturedCode.Should().Be(ValidCode);
    }

    [Fact]
    public async Task Submit_NoAuthenticatedUser_RedirectsToLogin()
    {
        var service = new Mock<ISurveyParticipationService>();
        var controller = BuildControllerWithoutUser(service);

        var result = await controller.Submit(ValidCode, ValidSurveyId, CancellationToken.None);

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task Submit_ServiceFailure_SetsTempDataErrorAndRedirectsToJoin()
    {
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.SubmitAsync(ValidUserId, ValidCode, ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Please answer all questions. 1 question(s) remaining."));

        var controller = BuildController(service);

        var result = await controller.Submit(ValidCode, ValidSurveyId, CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Join");
        redirect.RouteValues!["code"].Should().Be(ValidCode);
        controller.TempData["ErrorMessage"].Should()
            .Be("Please answer all questions. 1 question(s) remaining.");
    }

    [Fact]
    public async Task Submit_Success_SetsTempDataSuccessAndRedirectsToJoin()
    {
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.SubmitAsync(ValidUserId, ValidCode, ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);

        var result = await controller.Submit(ValidCode, ValidSurveyId, CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Join");
        redirect.RouteValues!["code"].Should().Be(ValidCode);
        controller.TempData["SuccessMessage"].Should().Be("Your answers have been submitted!");
    }

    [Fact]
    public async Task Submit_CallsServiceWithCorrectIds()
    {
        Guid capturedUserId = Guid.Empty;
        string? capturedCode = null;
        Guid capturedSurveyId = Guid.Empty;

        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.SubmitAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, Guid, CancellationToken>((uid, code, sid, _) =>
            {
                capturedUserId = uid;
                capturedCode = code;
                capturedSurveyId = sid;
            })
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);

        await controller.Submit(ValidCode, ValidSurveyId, CancellationToken.None);

        capturedUserId.Should().Be(ValidUserId);
        capturedCode.Should().Be(ValidCode);
        capturedSurveyId.Should().Be(ValidSurveyId);
    }

    [Fact]
    public async Task Submit_SurveyMismatch_SetsTempDataErrorAndRedirectsToJoin()
    {
        var wrongSurveyId = Guid.NewGuid();
        var service = new Mock<ISurveyParticipationService>();
        service
            .Setup(s => s.SubmitAsync(ValidUserId, ValidCode, wrongSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Survey mismatch."));

        var controller = BuildController(service);

        var result = await controller.Submit(ValidCode, wrongSurveyId, CancellationToken.None);

        controller.TempData["ErrorMessage"].Should().Be("Survey mismatch.");
    }
}
