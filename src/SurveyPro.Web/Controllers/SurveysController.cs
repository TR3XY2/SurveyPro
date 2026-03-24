// <copyright file="SurveysController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyPro.Application.DTOs.Surveys;
using SurveyPro.Application.Interfaces;
using SurveyPro.Web.ViewModels.Surveys;

/// <summary>
/// Surveys user flows.
/// </summary>
[Authorize]
public class SurveysController : Controller
{
    private readonly ISurveyService surveyService;
    private readonly ILogger<SurveysController> logger;

    public SurveysController(
        ISurveyService surveyService,
        ILogger<SurveysController> logger)
    {
        this.surveyService = surveyService;
        this.logger = logger;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await this.surveyService.GetPublicSurveysAsync(cancellationToken);
        if (result.IsFailure)
        {
            this.logger.LogWarning("Failed to load public surveys: {Error}", result.Error);
            TempData["ErrorMessage"] = result.Error;
            return View(Array.Empty<SurveyListItemDto>());
        }

        return View(result.Value ?? Array.Empty<SurveyListItemDto>());
    }

    [Authorize(Roles = "Author")]
    public async Task<IActionResult> My(CancellationToken cancellationToken)
    {
        var authorIdResult = this.GetCurrentUserId();
        if (authorIdResult.IsFailure)
        {
            return this.RedirectToAction("Login", "Account");
        }

        var result = await this.surveyService.GetMySurveysAsync(authorIdResult.Value, cancellationToken);
        if (result.IsFailure)
        {
            this.logger.LogWarning("Failed to load author's surveys: {Error}", result.Error);
            TempData["ErrorMessage"] = result.Error;
            return View(Array.Empty<SurveyListItemDto>());
        }

        return View(result.Value ?? Array.Empty<SurveyListItemDto>());
    }

    [Authorize(Roles = "Author")]
    public IActionResult Create()
    {
        return View(new CreateSurveyViewModel());
    }

    [Authorize(Roles = "Author")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSurveyViewModel model, CancellationToken cancellationToken)
    {
        if (!this.ModelState.IsValid)
        {
            return this.View(model);
        }

        var authorIdResult = this.GetCurrentUserId();
        if (authorIdResult.IsFailure)
        {
            return this.RedirectToAction("Login", "Account");
        }

        var request = new CreateSurveyRequestDto
        {
            Title = model.Title,
            Description = model.Description,
            IsPublic = model.IsPublic,
        };

        var result = await this.surveyService.CreateAsync(authorIdResult.Value, request, cancellationToken);
        if (result.IsFailure)
        {
            this.ModelState.AddModelError(string.Empty, result.Error);
            return this.View(model);
        }

        this.logger.LogInformation("Author {AuthorId} created survey {SurveyId}", authorIdResult.Value, result.Value);
        TempData["SuccessMessage"] = "Опитування створено.";
        return this.RedirectToAction(nameof(this.My));
    }

    private (bool IsFailure, Guid Value) GetCurrentUserId()
    {
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsed))
        {
            return (true, Guid.Empty);
        }

        return (false, parsed);
    }

    [Authorize(Roles = "Author")]
    [HttpPost]
    [ValidateAntiForgeryToken]
#pragma warning disable SA1202 // Elements should be ordered by access
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
#pragma warning restore SA1202 // Elements should be ordered by access
    {
        var authorIdResult = this.GetCurrentUserId();
        if (authorIdResult.IsFailure)
        {
            return this.RedirectToAction("Login", "Account");
        }

        var result = await this.surveyService.PublishAsync(id, authorIdResult.Value, cancellationToken);

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error;
            return this.RedirectToAction(nameof(this.My));
        }

        TempData["SuccessMessage"] = "Опитування опубліковано.";
        return this.RedirectToAction(nameof(this.My));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var authorIdResult = this.GetCurrentUserId();

        if (authorIdResult.IsFailure)
        {
            return this.RedirectToAction("Login", "Account");
        }

        var result = await this.surveyService.DeleteAsync(
            id,
            authorIdResult.Value,
            cancellationToken);

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error;
        }
        else
        {
            TempData["SuccessMessage"] = "Опитування видалено.";
        }

        return this.RedirectToAction(nameof(this.My));
    }
}
