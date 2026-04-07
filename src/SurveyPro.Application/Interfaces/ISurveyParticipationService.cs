// <copyright file="ISurveyParticipationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.Interfaces;

using SurveyPro.Application.Common;
using SurveyPro.Application.DTOs.Participation;

/// <summary>
/// Respondent survey flows.
/// </summary>
public interface ISurveyParticipationService
{
    Task<Result<SurveyParticipationDto>> GetByCodeAsync(string code, Guid? userId, CancellationToken cancellationToken);

    Task<Result> SaveDraftAsync(Guid userId, SaveDraftRequestDto request, CancellationToken cancellationToken);

    Task<Result> ClearDraftAsync(Guid userId, string accessCode, CancellationToken ct);

    Task<Result> SubmitAsync(Guid userId, string accessCode, Guid surveyId, CancellationToken ct);
}
