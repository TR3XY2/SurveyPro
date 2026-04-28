// <copyright file="ChartDataDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Charts;

/// <summary>
/// Data for pie chart visualization.
/// </summary>
public sealed class ChartDataDto
{
    public string QuestionId { get; set; } = string.Empty;

    public string QuestionText { get; set; } = string.Empty;

    public string QuestionType { get; set; } = string.Empty;

    public int QuestionOrderNumber { get; set; }

    public IReadOnlyCollection<ChartDataPoint> Labels { get; set; } = Array.Empty<ChartDataPoint>();

    public bool CanBeCharted { get; set; }

    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Single data point for chart.
/// </summary>
public sealed class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }

    public decimal Percentage { get; set; }
}
