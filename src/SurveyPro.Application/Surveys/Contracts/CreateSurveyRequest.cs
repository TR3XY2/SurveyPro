// <copyright file="CreateSurveyRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.Surveys.Contracts;

/// <summary>
/// Request model for creating a survey.
/// </summary>
public sealed class CreateSurveyRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPublic { get; set; }
}
