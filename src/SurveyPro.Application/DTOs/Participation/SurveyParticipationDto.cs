// <copyright file="SurveyParticipationDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Participation;

/// <summary>
/// Survey payload used by respondent views.
/// </summary>
public sealed class SurveyParticipationDto
{
    public Guid SurveyId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string AccessCode { get; set; } = string.Empty;

    public bool IsPublic { get; set; }

    public IReadOnlyCollection<ParticipationQuestionDto> Questions { get; set; } = Array.Empty<ParticipationQuestionDto>();

    public IReadOnlyCollection<ParticipationAnswerDto> DraftAnswers { get; set; } = Array.Empty<ParticipationAnswerDto>();
}
