// <copyright file="QuestionDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Questions;

public class QuestionDto
{
    public Guid Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int OrderNumber { get; set; }

    public List<string>? Options { get; set; }
}