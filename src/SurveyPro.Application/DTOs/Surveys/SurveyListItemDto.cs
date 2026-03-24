// <copyright file="SurveyListItemDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Surveys;

using SurveyPro.Domain.Enums;

/// <summary>
/// Survey item for lists.
/// </summary>
public sealed class SurveyListItemDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public SurveyStatuses Status { get; set; } = SurveyStatuses.Draft;

    public bool IsPublic { get; set; }

    public DateTime CreatedAt { get; set; }
}
