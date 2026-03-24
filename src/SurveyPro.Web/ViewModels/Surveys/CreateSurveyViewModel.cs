// <copyright file="CreateSurveyViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.ViewModels.Surveys;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Create survey form.
/// </summary>
public sealed class CreateSurveyViewModel
{
    [Required]
    [MaxLength(200)]
    [Display(Name = "Назва")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    [Display(Name = "Опис")]
    public string? Description { get; set; }

    [Display(Name = "Публічне опитування")]
    public bool IsPublic { get; set; }
}
