// <copyright file="QuestionsController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyPro.Application.DTOs.Questions;
using SurveyPro.Application.Interfaces;
using SurveyPro.Web.ViewModels.Questions;
using System.Security.Claims;

[Authorize(Roles = "Author")]
public class QuestionsController : BaseController
{
    private readonly IQuestionService questionService;

    public QuestionsController(IQuestionService questionService)
    {
        this.questionService = questionService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQuestionViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("Edit", "Surveys", new { id = model.SurveyId });
        }

        var userIdResult = GetCurrentUserId();
        if (userIdResult.IsFailure)
        {
            return RedirectToAction("Login", "Account");
        }

        var dto = new CreateQuestionRequestDto
        {
            SurveyId = model.SurveyId,
            Text = model.Text,
            Type = model.Type,
            Options = model.Options,
        };

        var result = await questionService.CreateAsync(userIdResult.Value, dto, cancellationToken);

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error;
        }
        else
        {
            TempData["SuccessMessage"] = "Question added";
        }

        return RedirectToAction("Edit", "Surveys", new { id = model.SurveyId });
    }
}
