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
/// Unit tests for the real HomeController.
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
        var result = BuildController().Index();

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Privacy_ReturnsView()
    {
        var result = BuildController().Privacy();

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public void Error_ReturnsViewWithErrorViewModel()
    {
        var result = BuildController().Error();

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeOfType<ErrorViewModel>();
    }

    [Fact]
    public void Error_ErrorViewModel_ShowRequestId_FalseWhenRequestIdNull()
    {
        var result = BuildController().Error();

        var model = result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<ErrorViewModel>().Subject;

        model.ShowRequestId.Should().BeTrue();
    }

    [Fact]
    public void Error_ErrorViewModel_ShowRequestId_TrueWhenRequestIdSet()
    {
        var model = new ErrorViewModel { RequestId = "test-id" };

        model.ShowRequestId.Should().BeTrue();
    }
}