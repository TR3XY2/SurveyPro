// <copyright file="AdminSurveysController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyPro.Application.Interfaces;
using SurveyPro.Web.Infrastructure.Filters;

/// <summary>
/// Admin controller for managing all surveys regardless of visibility.
/// </summary>
[Authorize(Roles = "Admin")]
public sealed class AdminSurveysController : Controller
{
    private readonly IAdminSurveyService adminSurveyService;
    private readonly ILogger<AdminSurveysController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminSurveysController"/> class.
    /// </summary>
    /// <param name="adminSurveyService">Admin survey service.</param>
    /// <param name="logger">Logger instance.</param>
    public AdminSurveysController(
        IAdminSurveyService adminSurveyService,
        ILogger<AdminSurveysController> logger)
    {
        this.adminSurveyService = adminSurveyService;
        this.logger = logger;
    }

    /// <summary>
    /// Displays all surveys for admin (public and private).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All surveys view.</returns>
    [RateLimit(15)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var surveys = await this.adminSurveyService.GetAllSurveysAsync(cancellationToken);

        this.logger.LogInformation(
            "Admin opened all surveys management page. Surveys count: {Count}",
            surveys.Count);

        return this.View(surveys);
    }

    /// <summary>
    /// Deletes any survey (admin only).
    /// </summary>
    /// <param name="id">Survey identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Redirect to index.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return this.View();
        }

        var deleted = await this.adminSurveyService.DeleteSurveyAsync(id, cancellationToken);

        if (deleted)
        {
            TempData["SuccessMessage"] = "Survey deleted successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Survey not found.";
        }

        return this.RedirectToAction(nameof(this.Index));
    }

    /// <summary>
    /// Shows a read-only list of survey questions for the selected survey.
    /// </summary>
    /// <param name="id">Survey identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Survey questions view.</returns>
    [RateLimit(15)]
    [HttpGet]
    public async Task<IActionResult> Questions(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return this.View();
        }

        var result = await this.adminSurveyService.GetSurveyQuestionsAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error;
            return this.RedirectToAction(nameof(this.Index));
        }

        return this.View(result.Value!);
    }

    /// <summary>
    /// Shows all submitted participant responses for selected survey.
    /// </summary>
    /// <param name="id">Survey identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Responses view.</returns>
    [RateLimit(15)]
    [HttpGet]
    public async Task<IActionResult> Responses(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return this.View();
        }

        var result = await this.adminSurveyService.GetSurveyResponsesAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return this.Content("ERROR: " + result.Error);
        }

        return this.View(result.Value!);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteResponse(Guid participantId, Guid surveyId, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return this.View();
        }

        var result = await this.adminSurveyService.DeleteParticipantResponseAsync(participantId, ct);

        if (result.IsFailure)
        {
            return Content("ERROR: " + result.Error);
        }

        return RedirectToAction("Responses", new { id = surveyId });
    }
}
