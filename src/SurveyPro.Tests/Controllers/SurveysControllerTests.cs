// <copyright file="SurveysControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Tests.Controllers;

using System;
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
/// Unit tests for <see cref="SurveysController"/> survey management actions.
/// </summary>
public class SurveysControllerTests
{
    private static readonly Guid ValidAuthorId = Guid.NewGuid();
    private static readonly Guid ValidSurveyId = Guid.NewGuid();

    private static SurveysController BuildController(
        Mock<ISurveyService> surveyService,
        Guid? userId = null)
    {
        var logger = new Mock<ILogger<SurveysController>>();
        var controller = new SurveysController(surveyService.Object, logger.Object);

        var actualUserId = userId ?? ValidAuthorId;
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, actualUserId.ToString()) };
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
        var controller = new SurveysController(surveyService.Object, logger.Object);

        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return controller;
    }

    // -------------------------------------------------------------------------
    // CREATE GET
    // -------------------------------------------------------------------------

    [Fact]
    public void Create_Get_ReturnsViewWithEmptyModel()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        var controller = BuildController(service);

        // Act
        var result = controller.Create();

        // Assert
        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeOfType<CreateSurveyViewModel>();
    }

    // -------------------------------------------------------------------------
    // CREATE POST
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_Post_InvalidModel_ReturnsView()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        var controller = BuildController(service);
        controller.ModelState.AddModelError("Title", "Required");
        var model = new CreateSurveyViewModel();

        // Act
        var result = await controller.Create(model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ViewResult>().Which.Model.Should().Be(model);
    }

    [Fact]
    public async Task Create_Post_NoAuthenticatedUser_RedirectsToLogin()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        var controller = BuildControllerWithoutUser(service);
        var model = new CreateSurveyViewModel { Title = "Test" };

        // Act
        var result = await controller.Create(model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task Create_Post_ServiceFailure_ReturnsViewWithError()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.CreateAsync(ValidAuthorId, It.IsAny<CreateSurveyRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Failure("Survey title is required."));

        var controller = BuildController(service);
        var model = new CreateSurveyViewModel { Title = "Test" };

        // Act
        var result = await controller.Create(model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Create_Post_Success_RedirectsToMy()
    {
        // Arrange
        var newId = Guid.NewGuid();
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.CreateAsync(ValidAuthorId, It.IsAny<CreateSurveyRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(newId));

        var controller = BuildController(service);
        var model = new CreateSurveyViewModel { Title = "My Survey", IsPublic = true };

        // Act
        var result = await controller.Create(model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("My");
        controller.TempData["SuccessMessage"].Should().Be("Survey created.");
    }

    [Fact]
    public async Task Create_Post_Success_PassesCorrectDtoToService()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        CreateSurveyRequestDto? captured = null;
        service
            .Setup(s => s.CreateAsync(ValidAuthorId, It.IsAny<CreateSurveyRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, CreateSurveyRequestDto, CancellationToken>((_, dto, _) => captured = dto)
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));

        var controller = BuildController(service);
        var model = new CreateSurveyViewModel
        {
            Title = "Annual Survey",
            Description = "A description",
            IsPublic = true,
        };

        // Act
        await controller.Create(model, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.Title.Should().Be("Annual Survey");
        captured.Description.Should().Be("A description");
        captured.IsPublic.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // EDIT GET
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Edit_Get_NoAuthenticatedUser_RedirectsToLogin()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        var controller = BuildControllerWithoutUser(service);

        // Act
        var result = await controller.Edit(ValidSurveyId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task Edit_Get_SurveyNotFound_RedirectsToMy()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetByIdAsync(ValidSurveyId, ValidAuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyListItemDto>.Failure("Survey not found."));

        var controller = BuildController(service);

        // Act
        var result = await controller.Edit(ValidSurveyId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("My");
        controller.TempData["ErrorMessage"].Should().Be("Survey not found.");
    }

    [Fact]
    public async Task Edit_Get_SurveyFound_ReturnsViewWithPopulatedModel()
    {
        // Arrange
        var dto = new SurveyListItemDto
        {
            Id = ValidSurveyId,
            Title = "Old Title",
            Description = "Old Desc",
            IsPublic = false,
        };

        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.GetByIdAsync(ValidSurveyId, ValidAuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SurveyListItemDto>.Success(dto));

        var controller = BuildController(service);

        // Act
        var result = await controller.Edit(ValidSurveyId, CancellationToken.None);

        // Assert
        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<EditSurveyViewModel>().Subject;
        model.Id.Should().Be(ValidSurveyId);
        model.Title.Should().Be("Old Title");
        model.Description.Should().Be("Old Desc");
        model.IsPublic.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // EDIT POST
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Edit_Post_InvalidModel_ReturnsView()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        var controller = BuildController(service);
        controller.ModelState.AddModelError("Title", "Required");
        var model = new EditSurveyViewModel { Id = ValidSurveyId };

        // Act
        var result = await controller.Edit(model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ViewResult>().Which.Model.Should().Be(model);
    }

    [Fact]
    public async Task Edit_Post_NoAuthenticatedUser_RedirectsToLogin()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        var controller = BuildControllerWithoutUser(service);
        var model = new EditSurveyViewModel { Id = ValidSurveyId, Title = "T" };

        // Act
        var result = await controller.Edit(model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task Edit_Post_ServiceFailure_ReturnsViewWithError()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.UpdateAsync(ValidSurveyId, ValidAuthorId, It.IsAny<UpdateSurveyRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Access denied."));

        var controller = BuildController(service);
        var model = new EditSurveyViewModel { Id = ValidSurveyId, Title = "Updated" };

        // Act
        var result = await controller.Edit(model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Edit_Post_Success_RedirectsToMy()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.UpdateAsync(ValidSurveyId, ValidAuthorId, It.IsAny<UpdateSurveyRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);
        var model = new EditSurveyViewModel { Id = ValidSurveyId, Title = "Updated Title" };

        // Act
        var result = await controller.Edit(model, CancellationToken.None);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("My");
        controller.TempData["SuccessMessage"].Should().Be("Survey updated.");
    }

    [Fact]
    public async Task Edit_Post_Success_PassesCorrectDtoToService()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        UpdateSurveyRequestDto? captured = null;
        service
            .Setup(s => s.UpdateAsync(ValidSurveyId, ValidAuthorId, It.IsAny<UpdateSurveyRequestDto>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, UpdateSurveyRequestDto, CancellationToken>((_, _, dto, _) => captured = dto)
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);
        var model = new EditSurveyViewModel
        {
            Id = ValidSurveyId,
            Title = "New Title",
            Description = "New Desc",
            IsPublic = true,
        };

        // Act
        await controller.Edit(model, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.Title.Should().Be("New Title");
        captured.Description.Should().Be("New Desc");
        captured.IsPublic.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // DELETE
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_NoAuthenticatedUser_RedirectsToLogin()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        var controller = BuildControllerWithoutUser(service);

        // Act
        var result = await controller.Delete(ValidSurveyId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task Delete_ServiceFailure_SetsTempDataErrorAndRedirectsToMy()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.DeleteAsync(ValidSurveyId, ValidAuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Survey not found"));

        var controller = BuildController(service);

        // Act
        var result = await controller.Delete(ValidSurveyId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("My");
        controller.TempData["ErrorMessage"].Should().Be("Survey not found");
    }

    [Fact]
    public async Task Delete_Success_SetsTempDataSuccessAndRedirectsToMy()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.DeleteAsync(ValidSurveyId, ValidAuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);

        // Act
        var result = await controller.Delete(ValidSurveyId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("My");
        controller.TempData["SuccessMessage"].Should().Be("Survey deleted.");
    }

    [Fact]
    public async Task Delete_CallsServiceWithCorrectIds()
    {
        // Arrange
        Guid capturedSurveyId = Guid.Empty;
        Guid capturedAuthorId = Guid.Empty;

        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, CancellationToken>((sid, aid, _) =>
            {
                capturedSurveyId = sid;
                capturedAuthorId = aid;
            })
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);

        // Act
        await controller.Delete(ValidSurveyId, CancellationToken.None);

        // Assert
        capturedSurveyId.Should().Be(ValidSurveyId);
        capturedAuthorId.Should().Be(ValidAuthorId);
    }

    // -------------------------------------------------------------------------
    // PUBLISH
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Publish_NoAuthenticatedUser_RedirectsToLogin()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        var controller = BuildControllerWithoutUser(service);

        // Act
        var result = await controller.Publish(ValidSurveyId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task Publish_ServiceFailure_SetsTempDataErrorAndRedirectsToMy()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.PublishAsync(ValidSurveyId, ValidAuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Survey not found"));

        var controller = BuildController(service);

        // Act
        var result = await controller.Publish(ValidSurveyId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("My");
        controller.TempData["ErrorMessage"].Should().Be("Survey not found");
    }

    [Fact]
    public async Task Publish_Success_SetsTempDataSuccessAndRedirectsToMy()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.PublishAsync(ValidSurveyId, ValidAuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);

        // Act
        var result = await controller.Publish(ValidSurveyId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("My");
        controller.TempData["SuccessMessage"].Should().Be("Survey published.");
    }

    [Fact]
    public async Task Publish_CallsServiceWithCorrectIds()
    {
        // Arrange
        Guid capturedSurveyId = Guid.Empty;
        Guid capturedAuthorId = Guid.Empty;

        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.PublishAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, CancellationToken>((sid, aid, _) =>
            {
                capturedSurveyId = sid;
                capturedAuthorId = aid;
            })
            .ReturnsAsync(Result.Success());

        var controller = BuildController(service);

        // Act
        await controller.Publish(ValidSurveyId, CancellationToken.None);

        // Assert
        capturedSurveyId.Should().Be(ValidSurveyId);
        capturedAuthorId.Should().Be(ValidAuthorId);
    }

    [Fact]
    public async Task Publish_AccessDenied_SetsTempDataErrorAndRedirectsToMy()
    {
        // Arrange
        var service = new Mock<ISurveyService>();
        service
            .Setup(s => s.PublishAsync(ValidSurveyId, ValidAuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Access denied"));

        var controller = BuildController(service);

        // Act
        var result = await controller.Publish(ValidSurveyId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("My");
        controller.TempData["ErrorMessage"].Should().Be("Access denied");
    }
}