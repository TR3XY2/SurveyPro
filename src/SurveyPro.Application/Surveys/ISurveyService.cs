// <copyright file="ISurveyService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.Surveys;

using SurveyPro.Application.Common;
using SurveyPro.Application.Surveys.Contracts;

/// <summary>
/// Survey use cases.
/// </summary>
public interface ISurveyService
{
    Task<Result<Guid>> CreateAsync(Guid authorId, CreateSurveyRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyCollection<SurveyListItemDto>>> GetMySurveysAsync(Guid authorId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyCollection<SurveyListItemDto>>> GetPublicSurveysAsync(CancellationToken cancellationToken);
}
