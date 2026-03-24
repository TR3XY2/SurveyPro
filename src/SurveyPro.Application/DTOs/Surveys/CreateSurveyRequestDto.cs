// <copyright file="CreateSurveyRequestDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Surveys;

/// <summary>
/// Request model for creating a survey.
/// </summary>
public sealed class CreateSurveyRequestDto
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPublic { get; set; }
}
