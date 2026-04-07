// <copyright file="ParticipationController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyPro.Application.DTOs.Participation;
using SurveyPro.Application.Interfaces;
using SurveyPro.Web.ViewModels.Participation;

public sealed class ParticipationController : BaseController
{
    private readonly ISurveyParticipationService surveyParticipationService;

    public ParticipationController(ISurveyParticipationService surveyParticipationService)
    {
        this.surveyParticipationService = surveyParticipationService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Join(string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["ErrorMessage"] = "Access code is required.";
            return this.RedirectToAction("Index", "Surveys");
        }

        var userId = this.GetCurrentUserId();
        Guid? currentUserId = userId.IsFailure ? null : userId.Value;

        var result = await this.surveyParticipationService.GetByCodeAsync(code, currentUserId, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error;
            return this.RedirectToAction("Index", "Surveys");
        }

        return this.View(this.MapToViewModel(result.Value!));
    }

    [Authorize(Roles = "Respondent")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(SurveyParticipationViewModel model, CancellationToken cancellationToken)
    {
        if (!this.ModelState.IsValid)
        {
            return this.View(model);
        }

        var userId = this.GetCurrentUserId();
        if (userId.IsFailure)
        {
            return this.RedirectToAction("Login", "Account");
        }

        var request = new SaveDraftRequestDto
        {
            SurveyId = model.SurveyId,
            AccessCode = model.AccessCode,
            Answers = model.Questions.Select(question => new ParticipationAnswerDto
            {
                QuestionId = question.QuestionId,
                TextAnswer = question.TextAnswer,
                SelectedOptionIds = this.GetSelectedOptionIds(question),
            }).ToList(),
        };

        var result = await this.surveyParticipationService.SaveDraftAsync(userId.Value, request, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error;
            return this.View(model);
        }

        TempData["SuccessMessage"] = "Draft saved.";
        return this.RedirectToAction(nameof(this.Join), new { code = model.AccessCode });
    }

    [Authorize(Roles = "Respondent")]
    [HttpPost]
    public async Task<IActionResult> Clear(string code, CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();

        if (userId.IsFailure)
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await this.surveyParticipationService.ClearDraftAsync(userId.Value, code, ct);

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error;
        }
        else
        {
            TempData["SuccessMessage"] = "All answers cleared.";
        }

        return RedirectToAction(nameof(this.Join), new { code });
    }

    [Authorize(Roles = "Respondent")]
    [HttpPost]
    public async Task<IActionResult> SaveDraft([FromBody] SurveyParticipationViewModel model, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        if (userId.IsFailure)
        {
            return Unauthorized();
        }

        var request = new SaveDraftRequestDto
        {
            SurveyId = model.SurveyId,
            AccessCode = model.AccessCode,
            Answers = model.Questions.Select(question => new ParticipationAnswerDto
            {
                QuestionId = question.QuestionId,
                TextAnswer = question.TextAnswer,
                SelectedOptionIds = this.GetSelectedOptionIds(question),
            }).ToList(),
        };

        var result = await this.surveyParticipationService.SaveDraftAsync(userId.Value, request, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok();
    }

    [Authorize(Roles = "Respondent")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(string accessCode, Guid surveyId, CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();
        if (userId.IsFailure)
        {
            return this.RedirectToAction("Login", "Account");
        }

        var result = await this.surveyParticipationService.SubmitAsync(
            userId.Value, accessCode, surveyId, ct);

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error;
            return this.RedirectToAction(nameof(this.Join), new { code = accessCode });
        }

        TempData["SuccessMessage"] = "Your answers have been submitted!";
        return this.RedirectToAction(nameof(this.Join), new { code = accessCode });
    }

    private SurveyParticipationViewModel MapToViewModel(SurveyParticipationDto dto)
    {
        var draftAnswers = dto.DraftAnswers.ToDictionary(answer => answer.QuestionId);

        return new SurveyParticipationViewModel
        {
            SurveyId = dto.SurveyId,
            AccessCode = dto.AccessCode,
            Title = dto.Title,
            Description = dto.Description,
            IsPublic = dto.IsPublic,
            IsSubmitted = dto.IsSubmitted,
            Questions = dto.Questions
                .OrderBy(question => question.OrderNumber)
                .Select(question =>
                {
                    draftAnswers.TryGetValue(question.QuestionId, out var draftAnswer);

                    return new ParticipationQuestionViewModel
                    {
                        QuestionId = question.QuestionId,
                        Text = question.Text,
                        Type = question.Type,
                        OrderNumber = question.OrderNumber,

                        Options = question.Options
                            .Select(option => new ParticipationOptionViewModel
                            {
                                Id = option.Id,
                                Text = option.Text,
                            })
                            .ToList(),
                        TextAnswer = draftAnswer?.TextAnswer,
                        SelectedOptionId = draftAnswer?.SelectedOptionIds.FirstOrDefault(),
                        SelectedOptionIds = draftAnswer?.SelectedOptionIds.ToList() ?? new List<Guid>(),
                    };
                })
                .ToList(),
        };
    }

    private List<Guid> GetSelectedOptionIds(ParticipationQuestionViewModel question)
    {
        if (string.Equals(question.Type, "MultipleChoice", StringComparison.OrdinalIgnoreCase))
        {
            return question.SelectedOptionIds
                .Where(optionId => optionId != Guid.Empty)
                .Distinct()
                .ToList();
        }

        if (question.SelectedOptionId.HasValue && question.SelectedOptionId.Value != Guid.Empty)
        {
            return new List<Guid> { question.SelectedOptionId.Value };
        }

        return new List<Guid>();
    }
}
