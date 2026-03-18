// <copyright file="HomeControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Tests.Controllers;

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SurveyPro.Web.Controllers;
using SurveyPro.Web.ViewModels;
using Xunit;

/// <summary>
/// Unit tests for <see cref="HomeController"/>.
/// </summary>
public class HomeControllerTests
{
    private static HomeController BuildController()
    {
        var controller = new HomeController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    [Fact]
    public void Index_ReturnsView()
    {
        // Arrange
        var controller = BuildController();

        // Act
        var result = controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Privacy_ReturnsView()
    {
        // Arrange
        var controller = BuildController();

        // Act
        var result = controller.Privacy();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Error_ReturnsViewWithErrorViewModel()
    {
        // Arrange
        var controller = BuildController();

        // Act
        var result = controller.Error();

        // Assert
        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeOfType<ErrorViewModel>();
    }

    [Fact]
    public void Error_ShouldSetRequestId_AndShowRequestIdIsTrue()
    {
        // Arrange
        var controller = BuildController();

        // Act
        var result = controller.Error();

        // Assert
        var model = result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<ErrorViewModel>().Subject;
        model.ShowRequestId.Should().BeTrue();
    }

    [Fact]
    public void Error_ErrorViewModel_ShowRequestId_TrueWhenRequestIdSet()
    {
        // Arrange
        var model = new ErrorViewModel { RequestId = "test-id" };

        // Act & Assert
        model.ShowRequestId.Should().BeTrue();
    }
}