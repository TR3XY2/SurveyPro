// <copyright file="IAdminSurveyService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.Interfaces;

using SurveyPro.Application.Common;
using SurveyPro.Application.DTOs.Surveys;

/// <summary>
/// Provides admin use cases for survey management.
/// </summary>
public interface IAdminSurveyService
{
    /// <summary>
    /// Returns all surveys (public and private) for admin page.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All surveys list.</returns>
    Task<IReadOnlyCollection<AdminSurveyListItemDto>> GetAllSurveysAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Deletes any survey by id (admin only).
    /// </summary>
    /// <param name="surveyId">Survey identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of deletion.</returns>
    Task<bool> DeleteSurveyAsync(Guid surveyId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns survey questions for admin read-only view.
    /// </summary>
    /// <param name="surveyId">Survey identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Survey question payload.</returns>
    Task<Result<AdminSurveyQuestionsDto>> GetSurveyQuestionsAsync(Guid surveyId, CancellationToken cancellationToken);

    Task<Result<AdminSurveyResponsesDto>> GetSurveyResponsesAsync(Guid surveyId, CancellationToken cancellationToken);
}