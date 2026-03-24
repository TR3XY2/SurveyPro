// <copyright file="ISurveyRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.Surveys;

using SurveyPro.Domain.Entities;

/// <summary>
/// Survey persistence abstraction.
/// </summary>
public interface ISurveyRepository
{
    Task AddAsync(Survey survey, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Survey>> GetByAuthorIdAsync(Guid authorId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Survey>> GetPublicAsync(CancellationToken cancellationToken);
}
