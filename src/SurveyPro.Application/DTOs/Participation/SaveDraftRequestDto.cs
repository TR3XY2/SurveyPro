// <copyright file="SaveDraftRequestDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Participation;

/// <summary>
/// Request payload for saving a draft response.
/// </summary>
public sealed class SaveDraftRequestDto
{
    public Guid SurveyId { get; set; }

    public string AccessCode { get; set; } = string.Empty;

    public IReadOnlyCollection<ParticipationAnswerDto> Answers { get; set; } = Array.Empty<ParticipationAnswerDto>();
}
