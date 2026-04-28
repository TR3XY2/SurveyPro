// <copyright file="AdminParticipantResponseDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Surveys;

public sealed class AdminParticipantResponseDto
{
    public Guid ParticipantId { get; set; }

    public string ParticipantName { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; }

    public List<AdminQuestionAnswerDto> Answers { get; set; } = new ();
}