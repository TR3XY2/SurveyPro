// <copyright file="ISurveyService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.Interfaces;

using SurveyPro.Application.Common;
using SurveyPro.Application.DTOs.Surveys;

/// <summary>
/// Survey use cases.
/// </summary>
public interface ISurveyService
{
    Task<Result<Guid>> CreateAsync(Guid authorId, CreateSurveyRequestDto request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyCollection<SurveyListItemDto>>> GetMySurveysAsync(Guid authorId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyCollection<SurveyListItemDto>>> GetPublicSurveysAsync(CancellationToken cancellationToken);

    Task<Result> PublishAsync(Guid surveyId, Guid authorId, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid surveyId, Guid authorId, CancellationToken cancellationToken);
}
