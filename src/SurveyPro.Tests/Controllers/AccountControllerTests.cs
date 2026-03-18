// <copyright file="AccountControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Tests.Controllers;

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
/// Unit tests for <see cref="AccountController"/> using mocked Identity services.
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
        // Arrange
        var controller = BuildController(BuildUserManager(), BuildSignInManager(BuildUserManager()));

        // Act
        var result = controller.Register();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Register_Post_InvalidModel_ReturnsViewWithModel()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var controller = BuildController(userMgr, BuildSignInManager(userMgr));
        controller.ModelState.AddModelError("Email", "Required");
        var model = new RegisterViewModel();

        // Act
        var result = await controller.Register(model);

        // Assert
        result.Should().BeOfType<ViewResult>().Which.Model.Should().Be(model);
    }

    [Fact]
    public async Task Register_Post_Success_Redirects()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);

        userMgr.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userMgr.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Respondent"))
            .ReturnsAsync(IdentityResult.Success);
        signMgr.Setup(x => x.SignInAsync(It.IsAny<ApplicationUser>(), false, null))
            .Returns(Task.CompletedTask);

        var controller = BuildController(userMgr, signMgr);
        var model = new RegisterViewModel
        {
            Name = "Alice", Email = "alice@example.com", Password = "Password123!",
        };

        // Act
        var result = await controller.Register(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task Register_Post_IdentityError_ReturnsViewWithErrors()
    {
        // Arrange
        var userMgr = BuildUserManager();
        userMgr.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email taken" }));

        var controller = BuildController(userMgr, BuildSignInManager(userMgr));
        var model = new RegisterViewModel
        {
            Name = "Bob", Email = "bob@example.com", Password = "Pass1!",
        };

        // Act
        var result = await controller.Register(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Login_Get_ReturnsView()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var controller = BuildController(userMgr, BuildSignInManager(userMgr));

        // Act
        var result = controller.Login();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Login_Post_InvalidModel_ReturnsView()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var controller = BuildController(userMgr, BuildSignInManager(userMgr));
        controller.ModelState.AddModelError("Email", "Required");

        // Act
        var result = await controller.Login(new LoginViewModel());

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Login_Post_UserNotFound_ReturnsViewWithError()
    {
        // Arrange
        var userMgr = BuildUserManager();
        userMgr.Setup(x => x.FindByEmailAsync("x@x.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var controller = BuildController(userMgr, BuildSignInManager(userMgr));

        // Act
        var result = await controller.Login(new LoginViewModel { Email = "x@x.com", Password = "p" });

        // Assert
        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Login_Post_WrongPassword_ReturnsViewWithError()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);
        var user = new ApplicationUser { UserName = "alice", Email = "alice@example.com" };

        userMgr.Setup(x => x.FindByEmailAsync("alice@example.com")).ReturnsAsync(user);
        signMgr.Setup(x => x.PasswordSignInAsync("alice", "bad", false, false))
            .ReturnsAsync(IdentitySignInResult.Failed);

        var controller = BuildController(userMgr, signMgr);

        // Act
        var result = await controller.Login(new LoginViewModel { Email = "alice@example.com", Password = "bad" });

        // Assert
        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Login_Post_Success_Redirects()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);
        var user = new ApplicationUser { UserName = "alice", Email = "alice@example.com" };

        userMgr.Setup(x => x.FindByEmailAsync("alice@example.com")).ReturnsAsync(user);
        signMgr.Setup(x => x.PasswordSignInAsync("alice", "Pass1!", false, false))
            .ReturnsAsync(IdentitySignInResult.Success);

        var controller = BuildController(userMgr, signMgr);

        // Act
        var result = await controller.Login(new LoginViewModel { Email = "alice@example.com", Password = "Pass1!" });

        // Assert
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task LogoutConfirmed_SignsOutAndRedirects()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);
        signMgr.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);

        var controller = BuildController(userMgr, signMgr);

        // Act
        var result = await controller.LogoutConfirmed();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task Profile_Get_UserFound_ReturnsViewWithPopulatedModel()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var user = new ApplicationUser { Name = "Alice", Email = "alice@example.com" };
        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var controller = BuildController(userMgr, BuildSignInManager(userMgr));

        // Act
        var result = await controller.Profile();

        // Assert
        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ProfileViewModel>().Subject;
        model.Name.Should().Be("Alice");
        model.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Profile_Get_UserNotFound_RedirectsToLogin()
    {
        // Arrange
        var userMgr = BuildUserManager();
        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        var controller = BuildController(userMgr, BuildSignInManager(userMgr));

        // Act
        var result = await controller.Profile();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public void ChangePassword_Get_ReturnsView()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var controller = BuildController(userMgr, BuildSignInManager(userMgr));

        // Act
        var result = controller.ChangePassword();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task ChangePassword_InvalidModel_ReturnsView()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var controller = BuildController(userMgr, BuildSignInManager(userMgr));
        controller.ModelState.AddModelError("k", "v");

        // Act
        var result = await controller.ChangePassword(new ChangePasswordViewModel());

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task ChangePassword_UserNotFound_RedirectsToLogin()
    {
        // Arrange
        var userMgr = BuildUserManager();
        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        var controller = BuildController(userMgr, BuildSignInManager(userMgr));
        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "a", NewPassword = "b", ConfirmNewPassword = "b",
        };

        // Act
        var result = await controller.ChangePassword(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task ChangePassword_Success_SetsTempDataAndRedirects()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);
        var user = new ApplicationUser { Email = "u@u.com" };

        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userMgr.Setup(x => x.ChangePasswordAsync(user, "Old1!", "New1!"))
            .ReturnsAsync(IdentityResult.Success);
        signMgr.Setup(x => x.RefreshSignInAsync(user))
            .Returns(Task.CompletedTask);

        var controller = BuildController(userMgr, signMgr);
        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "Old1!", NewPassword = "New1!", ConfirmNewPassword = "New1!",
        };

        // Act
        var result = await controller.ChangePassword(model);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ChangePassword");
        controller.TempData["SuccessMessage"].Should().Be("Password changed successfully.");
    }

    [Fact]
    public async Task ChangePassword_Failure_AddsErrorAndReturnsView()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);
        var user = new ApplicationUser { Email = "u@u.com" };

        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userMgr.Setup(x => x.ChangePasswordAsync(user, "Bad!", "New1!"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Wrong" }));

        var controller = BuildController(userMgr, signMgr);
        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "Bad!", NewPassword = "New1!", ConfirmNewPassword = "New1!",
        };

        // Act
        var result = await controller.ChangePassword(model);

        // Assert
        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EditProfile_Get_UserFound_ReturnsViewWithCurrentData()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var user = new ApplicationUser { Name = "Bob", Email = "bob@example.com" };
        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var controller = BuildController(userMgr, BuildSignInManager(userMgr));

        // Act
        var result = await controller.EditProfile();

        // Assert
        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<EditProfileViewModel>().Subject;
        model.Name.Should().Be("Bob");
        model.Email.Should().Be("bob@example.com");
    }

    [Fact]
    public async Task EditProfile_Get_UserNotFound_RedirectsToLogin()
    {
        // Arrange
        var userMgr = BuildUserManager();
        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        var controller = BuildController(userMgr, BuildSignInManager(userMgr));

        // Act
        var result = await controller.EditProfile();

        // Assert
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task EditProfile_InvalidModel_ReturnsView()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var controller = BuildController(userMgr, BuildSignInManager(userMgr));
        controller.ModelState.AddModelError("k", "v");

        // Act
        var result = await controller.EditProfile(new EditProfileViewModel());

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task EditProfile_Post_UserNotFound_RedirectsToLogin()
    {
        // Arrange
        var userMgr = BuildUserManager();
        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        var controller = BuildController(userMgr, BuildSignInManager(userMgr));

        // Act
        var result = await controller.EditProfile(new EditProfileViewModel { Name = "n", Email = "e@e.com" });

        // Assert
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task EditProfile_Success_SetsTempDataAndRedirects()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);
        var user = new ApplicationUser { Email = "u@u.com", Name = "Old" };

        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userMgr.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        signMgr.Setup(x => x.RefreshSignInAsync(user))
            .Returns(Task.CompletedTask);

        var controller = BuildController(userMgr, signMgr);

        // Act
        var result = await controller.EditProfile(new EditProfileViewModel { Name = "New", Email = "new@e.com" });

        // Assert
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("EditProfile");
        controller.TempData["SuccessMessage"].Should().Be("Profile updated successfully.");
    }

    [Fact]
    public async Task EditProfile_Failure_AddsErrorAndReturnsView()
    {
        // Arrange
        var userMgr = BuildUserManager();
        var signMgr = BuildSignInManager(userMgr);
        var user = new ApplicationUser { Email = "u@u.com", Name = "Old" };

        userMgr.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userMgr.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Fail" }));

        var controller = BuildController(userMgr, signMgr);

        // Act
        var result = await controller.EditProfile(new EditProfileViewModel { Name = "New", Email = "new@e.com" });

        // Assert
        result.Should().BeOfType<ViewResult>();
        controller.ModelState.IsValid.Should().BeFalse();
    }
}