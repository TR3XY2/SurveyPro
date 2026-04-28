// <copyright file="AdminQuestionAnswerDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Surveys;

public sealed class AdminQuestionAnswerDto
{
    public Guid QuestionId { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public string QuestionType { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;
}