// <copyright file="EditSurveyViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.ViewModels.Surveys;

using SurveyPro.Application.DTOs.Questions;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Edit survey form.
/// </summary>
public sealed class EditSurveyViewModel
{
    public Guid Id { get; set; }

    public string AccessCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Display(Name = "Name")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Public Survey")]
    public bool IsPublic { get; set; }

    public List<QuestionDto> Questions { get; set; } = new ();
}