// <copyright file="AccountController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SurveyPro.Domain.Entities;
using SurveyPro.Web.ViewModels;

/// <summary>
/// Controller responsible for user account management.
/// Handles registration, profile viewing/editing, password changes, and account deletion.
/// </summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly ILogger<AccountController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountController"/> class.
    /// </summary>
    /// <param name="userManager">The user manager instance.</param>
    /// <param name="signInManager">The sign-in manager instance.</param>
    /// <param name="logger">The logger instance.</param>
    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AccountController> logger)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.logger = logger;
    }

    /// <summary>
    /// Shows the registration page.
    /// </summary>
    /// <returns>The registration view.</returns>
    public IActionResult Register()
    {
        return View();
    }

    /// <summary>
    /// Handles user registration.
    /// </summary>
    /// <param name="model">The registration model containing user data.</param>
    /// <returns>Redirects on success, or returns the view with errors.</returns>
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Name,
            Email = model.Email,
            Name = model.Name,
        };

        var result = await this.userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            this.logger.LogInformation("User registered {Email}", model.Email);

            await this.userManager.AddToRoleAsync(user, "Respondent");

            await this.signInManager.SignInAsync(user, false);

            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    /// <summary>
    /// Shows the current user's profile.
    /// </summary>
    /// <returns>The profile view.</returns>
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await this.userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login");
        }

        var roles = await this.userManager.GetRolesAsync(user);

        var model = new ProfileViewModel
        {
            Name = user.Name,
            Email = user.Email ?? string.Empty,
            Roles = roles,
        };

        return View(model);
    }

    /// <summary>
    /// Shows the profile edit page.
    /// </summary>
    /// <returns>The edit profile view.</returns>
    [Authorize]
    public async Task<IActionResult> EditProfile()
    {
        var user = await this.userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login");
        }

        var model = new EditProfileViewModel
        {
            Name = user.Name,
            Email = user.Email ?? string.Empty,
        };

        return View(model);
    }

    /// <summary>
    /// Handles profile editing.
    /// </summary>
    /// <param name="model">The edit profile model.</param>
    /// <returns>Redirects on success, or returns the view with errors.</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> EditProfile(EditProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await this.userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login");
        }

        user.Name = model.Name;
        user.Email = model.Email;
        user.UserName = model.Email;

        var result = await this.userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            await this.signInManager.RefreshSignInAsync(user);
            this.logger.LogInformation("User {Email} updated profile", user.Email);
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(EditProfile));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    /// <summary>
    /// Shows the change password page.
    /// </summary>
    /// <returns>The change password view.</returns>
    [Authorize]
    public IActionResult ChangePassword() => View();

    /// <summary>
    /// Handles the change password request.
    /// </summary>
    /// <param name="model">The change password model.</param>
    /// <returns>Redirects on success, or returns the view with errors.</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await this.userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login");
        }

        var result = await this.userManager.ChangePasswordAsync(
            user, model.CurrentPassword, model.NewPassword);

        if (result.Succeeded)
        {
            await this.signInManager.RefreshSignInAsync(user);
            this.logger.LogInformation("User {Email} changed password", user.Email);
            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction(nameof(ChangePassword));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    /// <summary>
    /// Shows the current user's profile.
    /// </summary>
    /// <returns>The profile view.</returns>
    public IActionResult Login()
    {
        return View();
    }

    /// <summary>
    /// Shows the current user's profile.
    /// </summary>
    /// <returns>The profile view.</returns>
    [HttpPost]

    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await this.userManager.FindByEmailAsync(model.Email);

        if (user == null || string.IsNullOrEmpty(user.UserName))
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt");
            return View(model);
        }

        var result = await this.signInManager.PasswordSignInAsync(
            user.UserName,
            model.Password,
            model.RememberMe,
            false);

        if (result.Succeeded)
        {
            this.logger.LogInformation("User logged in {Email}", model.Email);
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt");
        return View(model);
    }

    /// <summary>
    /// Shows the current user's profile.
    /// </summary>
    /// <returns>The profile view.</returns>
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await this.userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login");
        }

        var roles = await this.userManager.GetRolesAsync(user);

        var model = new ProfileViewModel
        {
            Name = user.Name,
            Email = user.Email ?? string.Empty,
            Roles = roles,
        };

        return View(model);
    }

    /// <summary>
    /// Shows the profile edit page.
    /// </summary>
    /// <returns>The edit profile view.</returns>
    [Authorize]
    public async Task<IActionResult> EditProfile()
    {
        var user = await this.userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login");
        }

        var model = new EditProfileViewModel
        {
            Name = user.Name,
            Email = user.Email ?? string.Empty,
        };

        return View(model);
    }

    /// <summary>
    /// Handles profile editing.
    /// </summary>
    /// <param name="model">The edit profile model.</param>
    /// <returns>Redirects on success, or returns the view with errors.</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> EditProfile(EditProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await this.userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login");
        }

        user.Name = model.Name;
        user.Email = model.Email;
        user.UserName = model.Email;

        var result = await this.userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            await this.signInManager.RefreshSignInAsync(user);
            this.logger.LogInformation("User {Email} updated profile", user.Email);
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(EditProfile));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

        return View(model);
    }

    /// <summary>
    /// Shows the account deletion confirmation page.
    /// </summary>
    /// <returns>The delete account view.</returns>
    [Authorize]
    public IActionResult DeleteAccount() => View();

    /// <summary>
    /// Handles account deletion after confirmation.
    /// </summary>
    /// <returns>Redirects to home on success, or back to the delete page on failure.</returns>
    [HttpPost]
    [ActionName("DeleteAccount")]
    [Authorize]
    public async Task<IActionResult> DeleteAccountConfirmed()
    {
        var user = await this.userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login");
        }

        await this.signInManager.SignOutAsync();
        var result = await this.userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            this.logger.LogInformation("User {Email} deleted account", user.Email);
            return RedirectToAction("Index", "Home");
        }

        TempData["ErrorMessage"] = "Failed to delete account.";
        return RedirectToAction(nameof(DeleteAccount));
    }
}
