// <copyright file="ParticipationOptionDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Participation;

/// <summary>
/// Answer option displayed to a respondent.
/// </summary>
public sealed class ParticipationOptionDto
{
    public Guid Id { get; set; }

    public string Text { get; set; } = string.Empty;
}
