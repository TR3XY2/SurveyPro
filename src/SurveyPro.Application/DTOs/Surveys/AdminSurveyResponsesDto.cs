// <copyright file="AdminSurveyResponsesDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Surveys;

public sealed class AdminSurveyResponsesDto
{
    public Guid SurveyId { get; set; }

    public string SurveyTitle { get; set; } = string.Empty;

    public int TotalParticipants { get; set; }

    public List<AdminParticipantResponseDto> Participants { get; set; } = new ();
}