// <copyright file="IChartService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.Interfaces;

using SurveyPro.Application.Common;
using SurveyPro.Application.DTOs.Charts;

/// <summary>
/// Service for generating chart and histogram data from survey responses.
/// </summary>
public interface IChartService
{
    /// <summary>
    /// Generate chart data for all questions in a survey.
    /// </summary>
    /// <param name="surveyId">The survey ID.</param>
    /// <param name="requestedByUserId">The user requesting the data.</param>
    /// <param name="isAdministrator">Whether the user is an administrator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Chart data for the survey.</returns>
    Task<Result<AnswerChartsDto>> GetSurveyChartsAsync(
        Guid surveyId,
        Guid requestedByUserId,
        bool isAdministrator,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get histogram data for a specific question.
    /// </summary>
    /// <param name="questionId">The question ID.</param>
    /// <param name="surveyId">The survey ID (for authorization).</param>
    /// <param name="requestedByUserId">The user requesting the data.</param>
    /// <param name="isAdministrator">Whether the user is an administrator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Histogram data for the question.</returns>
    Task<Result<HistogramDataDto>> GetQuestionHistogramAsync(
        Guid questionId,
        Guid surveyId,
        Guid requestedByUserId,
        bool isAdministrator,
        CancellationToken cancellationToken);
}
