// <copyright file="AccountController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.Controllers;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SurveyPro.Domain.Entities;
using SurveyPro.Web.ViewModels;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly ILogger<AccountController> logger;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AccountController> logger)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.logger = logger;
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
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
}
