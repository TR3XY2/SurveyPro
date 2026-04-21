// <copyright file="AdminSurveyListItemDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Surveys;

using SurveyPro.Domain.Enums;

/// <summary>
/// Survey item for admin list (all surveys, public and private).
/// </summary>
public sealed class AdminSurveyListItemDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public SurveyStatuses Status { get; set; }

    public bool IsPublic { get; set; }

    public DateTime CreatedAt { get; set; }

    public string AccessCode { get; set; } = string.Empty;

    public string AuthorName { get; set; } = string.Empty;

    public string AuthorEmail { get; set; } = string.Empty;

    public int QuestionCount { get; set; }

    public int ResponseCount { get; set; }
}