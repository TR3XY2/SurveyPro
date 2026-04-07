// <copyright file="ParticipationQuestionDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Participation;

/// <summary>
/// Survey question shown to a respondent.
/// </summary>
public sealed class ParticipationQuestionDto
{
    public Guid QuestionId { get; set; }

    public string Text { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int OrderNumber { get; set; }

    public IReadOnlyCollection<ParticipationOptionDto> Options { get; set; } = Array.Empty<ParticipationOptionDto>();
}
