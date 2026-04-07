// <copyright file="ParticipationAnswerDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Participation;

/// <summary>
/// Draft answer payload for a respondent.
/// </summary>
public sealed class ParticipationAnswerDto
{
    public Guid QuestionId { get; set; }

    public string? TextAnswer { get; set; }

    public List<Guid> SelectedOptionIds { get; set; } = new ();
}
