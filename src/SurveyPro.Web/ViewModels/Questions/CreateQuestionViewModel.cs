// <copyright file="CreateQuestionViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.ViewModels.Questions;

using System.ComponentModel.DataAnnotations;

public class CreateQuestionViewModel
{
    [Required]
    public Guid SurveyId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = "Text";

    public List<string> Options { get; set; } = new List<string>();
}
