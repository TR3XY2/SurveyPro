// <copyright file="AccountControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Tests.Controllers;

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyPro.Domain.Entities;
using SurveyPro.Web.Controllers;
using SurveyPro.Web.ViewModels;
using Xunit;

using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;
/// <summary>
/// Unit tests for the real AccountController using mocked Identity services.
/// These tests exercise actual production code so SonarQube reports real coverage.
/// </summary>
public class AccountControllerTests
{
    private static Mock<UserManager<ApplicationUser>> BuildUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<SignInManager<ApplicationUser>> BuildSignInManager(
        Mock<UserManager<ApplicationUser>> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        return new Mock<SignInManager<ApplicationUser>>(
            userManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null!, null!, null!, null!);
    }

    private static AccountController BuildController(
        Mock<UserManager<ApplicationUser>> userManager,
        Mock<SignInManager<ApplicationUser>> signInManager)
    {
        var logger = new Mock<ILogger<AccountController>>();
        var controller = new AccountController(
            userManager.Object, signInManager.Object, logger.Object);

        var httpContext = new DefaultHttpContext();
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return controller;
    }

    [Fact]
    public void Register_Get_ReturnsView()
    {
        var controller = BuildController(BuildUserManager(), BuildSignInManager(BuildUserManager()));
        controller.Register().Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Register_Post_InvalidModel_ReturnsViewWithModel()
    {
        var userMgr = BuildUserManager();
        var controller = BuildController(userMgr, BuildSignInManager(userMgr));
        controller.ModelState.AddModelError("Email", "Required");

        var model = new RegisterViewModel();
        var result = await controller.Register(model);

        result.Should().BeOfType<ViewResult>().Which.Model.Should().Be(model);
    }

    [Fact]
    public async Task Register_Post_Success_Redirects()
    {
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);

        userMgr.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userMgr.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Respondent"))
            .ReturnsAsync(IdentityResult.Success);
        signMgr.Setup(x => x.SignInAsync(It.IsAny<ApplicationUser>(), false, null))
            .Returns(Task.CompletedTask);

        var controller = BuildController(userMgr, signMgr);
        var result = await controller.Register(new RegisterViewModel
        {
            Name = "Alice", Email = "alice@example.com", Password = "Password123!",
        });

        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task Register_Post_IdentityError_ReturnsViewWithErrors()
    {
        var userMgr = BuildUserManager();
        userMgr.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email taken" }));

        var controller = BuildController(userMgr, BuildSignInManager(userMgr));
        var result = await controller.Register(new RegisterViewModel
        {
            Name = "Bob", Email = "bob@example.com", Password = "Pass1!",
        });

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Login_Get_ReturnsView()
    {
        var userMgr = BuildUserManager();
        BuildController(userMgr, BuildSignInManager(userMgr)).Login()
            .Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Login_Post_InvalidModel_ReturnsView()
    {
        var userMgr = BuildUserManager();
        var controller = BuildController(userMgr, BuildSignInManager(userMgr));
        controller.ModelState.AddModelError("Email", "Required");

        var result = await controller.Login(new LoginViewModel());
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Login_Post_UserNotFound_ReturnsViewWithError()
    {
        var userMgr = BuildUserManager();
        userMgr.Setup(x => x.FindByEmailAsync("x@x.com")).ReturnsAsync((ApplicationUser?)null);

        var controller = BuildController(userMgr, BuildSignInManager(userMgr));
        var result = await controller.Login(new LoginViewModel { Email = "x@x.com", Password = "p" });

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Login_Post_WrongPassword_ReturnsViewWithError()
    {
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);
        var user = new ApplicationUser { UserName = "alice", Email = "alice@example.com" };
        userMgr.Setup(x => x.FindByEmailAsync("alice@example.com")).ReturnsAsync(user);
        signMgr.Setup(x => x.PasswordSignInAsync("alice", "bad", false, false))
            .ReturnsAsync(IdentitySignInResult.Failed);

        var controller = BuildController(userMgr, signMgr);
        var result = await controller.Login(new LoginViewModel { Email = "alice@example.com", Password = "bad" });

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Login_Post_Success_Redirects()
    {
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);
        var user = new ApplicationUser { UserName = "alice", Email = "alice@example.com" };
        userMgr.Setup(x => x.FindByEmailAsync("alice@example.com")).ReturnsAsync(user);
        signMgr.Setup(x => x.PasswordSignInAsync("alice", "Pass1!", false, false))
            .ReturnsAsync(IdentitySignInResult.Success);

        var controller = BuildController(userMgr, signMgr);
        var result = await controller.Login(new LoginViewModel { Email = "alice@example.com", Password = "Pass1!" });

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task LogoutConfirmed_SignsOutAndRedirects()
    {
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);
        signMgr.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);

        var result = await BuildController(userMgr, signMgr).LogoutConfirmed();

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public void ChangePassword_Get_ReturnsView()
    {
        var userMgr = BuildUserManager();
        BuildController(userMgr, BuildSignInManager(userMgr)).ChangePassword()
            .Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task ChangePassword_InvalidModel_ReturnsView()
    {
        var userMgr = BuildUserManager();
        var controller = BuildController(userMgr, BuildSignInManager(userMgr));
        controller.ModelState.AddModelError("k", "v");

        var result = await controller.ChangePassword(new ChangePasswordViewModel());
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task ChangePassword_UserNotFound_RedirectsToLogin()
    {
        var userMgr = BuildUserManager();
        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await BuildController(userMgr, BuildSignInManager(userMgr))
            .ChangePassword(new ChangePasswordViewModel { CurrentPassword = "a", NewPassword = "b", ConfirmNewPassword = "b" });

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task ChangePassword_Success_SetsTempDataAndRedirects()
    {
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);
        var user = new ApplicationUser { Email = "u@u.com" };
        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);
        userMgr.Setup(x => x.ChangePasswordAsync(user, "Old1!", "New1!")).ReturnsAsync(IdentityResult.Success);
        signMgr.Setup(x => x.RefreshSignInAsync(user)).Returns(Task.CompletedTask);

        var controller = BuildController(userMgr, signMgr);
        var result = await controller.ChangePassword(new ChangePasswordViewModel
        {
            CurrentPassword = "Old1!", NewPassword = "New1!", ConfirmNewPassword = "New1!",
        });

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ChangePassword");
        controller.TempData["SuccessMessage"].Should().Be("Password changed successfully.");
    }

    [Fact]
    public async Task ChangePassword_Failure_AddsErrorAndReturnsView()
    {
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);
        var user = new ApplicationUser { Email = "u@u.com" };
        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);
        userMgr.Setup(x => x.ChangePasswordAsync(user, "Bad!", "New1!"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Wrong" }));

        var controller = BuildController(userMgr, signMgr);
        var result = await controller.ChangePassword(new ChangePasswordViewModel
        {
            CurrentPassword = "Bad!", NewPassword = "New1!", ConfirmNewPassword = "New1!",
        });

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EditProfile_InvalidModel_ReturnsView()
    {
        var userMgr = BuildUserManager();
        var controller = BuildController(userMgr, BuildSignInManager(userMgr));
        controller.ModelState.AddModelError("k", "v");

        var result = await controller.EditProfile(new EditProfileViewModel());
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task EditProfile_UserNotFound_RedirectsToLogin()
    {
        var userMgr = BuildUserManager();
        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await BuildController(userMgr, BuildSignInManager(userMgr))
            .EditProfile(new EditProfileViewModel { Name = "n", Email = "e@e.com" });

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task EditProfile_Success_SetsTempDataAndRedirects()
    {
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);
        var user = new ApplicationUser { Email = "u@u.com", Name = "Old" };
        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);
        userMgr.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        signMgr.Setup(x => x.RefreshSignInAsync(user)).Returns(Task.CompletedTask);

        var controller = BuildController(userMgr, signMgr);
        var result = await controller.EditProfile(new EditProfileViewModel { Name = "New", Email = "new@e.com" });

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("EditProfile");
        controller.TempData["SuccessMessage"].Should().Be("Profile updated successfully.");
    }

    [Fact]
    public async Task EditProfile_Failure_AddsErrorAndReturnsView()
    {
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);
        var user = new ApplicationUser { Email = "u@u.com", Name = "Old" };
        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);
        userMgr.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Fail" }));

        var controller = BuildController(userMgr, signMgr);
        var result = await controller.EditProfile(new EditProfileViewModel { Name = "New", Email = "new@e.com" });

        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
    }
}