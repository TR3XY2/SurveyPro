// <copyright file="AnswerChartsDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Charts;

/// <summary>
/// Combined chart and histogram data for all questions in a survey.
/// </summary>
public sealed class AnswerChartsDto
{
    public Guid SurveyId { get; set; }

    public string SurveyTitle { get; set; } = string.Empty;

    public int TotalSubmittedResponses { get; set; }

    public IReadOnlyCollection<ChartDataDto> Charts { get; set; } = Array.Empty<ChartDataDto>();

    public IReadOnlyCollection<HistogramDataDto> Histograms { get; set; } = Array.Empty<HistogramDataDto>();
}
