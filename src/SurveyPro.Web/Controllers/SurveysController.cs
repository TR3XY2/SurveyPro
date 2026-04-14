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
using SurveyPro.Web.Services;

/// <summary>
/// Surveys user flows.
/// </summary>
[Authorize]
public class SurveysController : BaseController
{
    private readonly ISurveyService surveyService;
    private readonly ILogger<SurveysController> logger;
    private readonly IQuestionService questionService;

    public SurveysController(
        ISurveyService surveyService,
        ILogger<SurveysController> logger,
        IQuestionService questionService)
    {
        this.surveyService = surveyService;
        this.logger = logger;
        this.questionService = questionService;
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

    [Authorize(Roles = "Author,Admin")]
    [HttpGet]
    public async Task<IActionResult> ExportResponsesCsv(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId.IsFailure)
        {
            return this.RedirectToAction("Login", "Account");
        }

        var result = await this.surveyService.GetSurveyResponsesAsync(
            id,
            currentUserId.Value,
            this.User.IsInRole("Admin"),
            cancellationToken);

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error;
            return this.RedirectToAction(nameof(this.Responses), new { id });
        }

        var viewModel = this.MapToResponsesViewModel(result.Value!);

        var csvBytes = SurveyCsvExporter.GenerateResponsesCsv(viewModel);

        return File(
            csvBytes,
            "text/csv",
            $"SurveyResults_{viewModel.SurveyTitle}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
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
        TempData["SuccessMessage"] = "Survey created.";

        return this.RedirectToAction("Edit", new { id = result.Value });
    }

    [Authorize(Roles = "Author")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (!this.ModelState.IsValid)
        {
            return this.View();
        }

        var authorIdResult = this.GetCurrentUserId();
        if (authorIdResult.IsFailure)
        {
            return this.RedirectToAction("Login", "Account");
        }

        var result = await this.surveyService.GetByIdAsync(id, authorIdResult.Value, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error;
            return this.RedirectToAction(nameof(this.My));
        }

        var survey = result.Value!;

        var questions = await this.questionService.GetBySurveyIdAsync(id, cancellationToken);

        return this.View(new EditSurveyViewModel
        {
            Id = survey.Id,
            AccessCode = survey.AccessCode,
            Title = survey.Title,
            Description = survey.Description,
            IsPublic = survey.IsPublic,
            Questions = questions,
        });
    }

    [Authorize(Roles = "Author")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditSurveyViewModel model, CancellationToken cancellationToken)
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

        var request = new UpdateSurveyRequestDto
        {
            Title = model.Title,
            Description = model.Description,
            IsPublic = model.IsPublic,
        };

        var result = await this.surveyService.UpdateAsync(model.Id, authorIdResult.Value, request, cancellationToken);
        if (result.IsFailure)
        {
            this.ModelState.AddModelError(string.Empty, result.Error);
            return this.View(model);
        }

        TempData["SuccessMessage"] = "Survey updated.";
        return this.RedirectToAction(nameof(this.My));
    }

    [Authorize(Roles = "Author")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        if (!this.ModelState.IsValid)
        {
            return this.View();
        }

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

        TempData["SuccessMessage"] = "Survey published.";
        return this.RedirectToAction(nameof(this.My));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!this.ModelState.IsValid)
        {
            return this.View();
        }

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
            TempData["SuccessMessage"] = "Survey deleted.";
        }

        return this.RedirectToAction(nameof(this.My));
    }

    [Authorize(Roles = "Author")]
    [HttpPost]
    public async Task<IActionResult> SaveDraft([FromBody] EditSurveyViewModel model, CancellationToken cancellationToken)
    {
        if (!this.ModelState.IsValid)
        {
            return this.View(model);
        }

        var authorIdResult = this.GetCurrentUserId();
        if (authorIdResult.IsFailure)
        {
            return Unauthorized();
        }

        var request = new UpdateSurveyRequestDto
        {
            Title = model.Title,
            Description = model.Description,
            IsPublic = model.IsPublic,
        };

        var result = await this.surveyService.UpdateAsync(model.Id, authorIdResult.Value, request, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok();
    }

    [Authorize(Roles = "Author,Admin")]
    [HttpGet]
    public async Task<IActionResult> Responses(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId.IsFailure)
        {
            return this.RedirectToAction("Login", "Account");
        }

        var result = await this.surveyService.GetSurveyResponsesAsync(
            id,
            currentUserId.Value,
            this.User.IsInRole("Admin"),
            cancellationToken);

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error;

            if (this.User.IsInRole("Author"))
            {
                return this.RedirectToAction(nameof(this.My));
            }

            return this.RedirectToAction(nameof(this.Index));
        }

        return this.View(this.MapToResponsesViewModel(result.Value!));
    }

    [Authorize(Roles = "Author,Admin")]
    [HttpGet]
    public async Task<IActionResult> ExportResponsesPdf(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId.IsFailure)
        {
            return this.RedirectToAction("Login", "Account");
        }

        var result = await this.surveyService.GetSurveyResponsesAsync(
            id,
            currentUserId.Value,
            this.User.IsInRole("Admin"),
            cancellationToken);

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error;
            return this.RedirectToAction(nameof(this.Responses), new { id });
        }

        var viewModel = this.MapToResponsesViewModel(result.Value!);

        var pdfBytes = SurveyPro.Web.Services.SurveyPdfExporter.GenerateResponsesPdf(viewModel);

        return File(
            pdfBytes,
            "application/pdf",
            $"SurveyResults_{viewModel.SurveyTitle}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
    }

    private SurveyResponsesViewModel MapToResponsesViewModel(SurveyResponsesDto dto)
    {
        return new SurveyResponsesViewModel
        {
            SurveyId = dto.SurveyId,
            SurveyTitle = dto.SurveyTitle,
            SurveyDescription = dto.SurveyDescription,
            AccessCode = dto.AccessCode,
            TotalSubmittedResponses = dto.TotalSubmittedResponses,
            Responses = dto.Responses
                .OrderByDescending(response => response.SubmittedAt)
                .Select(response => new SurveyResponseViewModel
                {
                    ResponseId = response.ResponseId,
                    RespondentUserId = response.RespondentUserId,
                    RespondentName = response.RespondentName,
                    RespondentEmail = response.RespondentEmail,
                    SubmittedAt = response.SubmittedAt,
                    Answers = response.Answers
                        .OrderBy(answer => answer.QuestionOrderNumber)
                        .Select(answer => new SurveyResponseAnswerViewModel
                        {
                            QuestionId = answer.QuestionId,
                            QuestionOrderNumber = answer.QuestionOrderNumber,
                            QuestionText = answer.QuestionText,
                            QuestionType = answer.QuestionType,
                            TextAnswer = answer.TextAnswer,
                            SelectedOptionIds = answer.SelectedOptionIds.ToList(),
                            SelectedOptionTexts = answer.SelectedOptionTexts.ToList(),
                        })
                        .ToList(),
                })
                .ToList(),
        };
    }
}
