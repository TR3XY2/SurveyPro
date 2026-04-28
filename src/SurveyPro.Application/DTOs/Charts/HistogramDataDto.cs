// <copyright file="HistogramDataDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Charts;

/// <summary>
/// Data for histogram visualization.
/// </summary>
public sealed class HistogramDataDto
{
    public string QuestionId { get; set; } = string.Empty;

    public string QuestionText { get; set; } = string.Empty;

    public string QuestionType { get; set; } = string.Empty;

    public int QuestionOrderNumber { get; set; }

    public IReadOnlyCollection<HistogramBucket> Buckets { get; set; } = Array.Empty<HistogramBucket>();

    public int TotalResponses { get; set; }

    public bool CanBeCharted { get; set; }

    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Single bucket/bar in histogram.
/// </summary>
public sealed class HistogramBucket
{
    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }

    public decimal Percentage { get; set; }
}
