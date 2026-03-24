// <copyright file="SurveyService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.Surveys;

using Microsoft.Extensions.Logging;
using SurveyPro.Application.Common;
using SurveyPro.Application.Surveys.Contracts;
using SurveyPro.Domain.Entities;

/// <summary>
/// Survey use-case service.
/// </summary>
public sealed class SurveyService : ISurveyService
{
    private readonly ISurveyRepository surveyRepository;
    private readonly ILogger<SurveyService> logger;

    public SurveyService(
        ISurveyRepository surveyRepository,
        ILogger<SurveyService> logger)
    {
        this.surveyRepository = surveyRepository;
        this.logger = logger;
    }

    public async Task<Result<Guid>> CreateAsync(
        Guid authorId,
        CreateSurveyRequest request,
        CancellationToken cancellationToken)
    {
        if (authorId == Guid.Empty)
        {
            return Result<Guid>.Failure("Invalid author id.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result<Guid>.Failure("Survey title is required.");
        }

        var survey = new Survey
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = "Draft",
            IsPublic = request.IsPublic,
            CreatedAt = DateTime.UtcNow,
        };

        await this.surveyRepository.AddAsync(survey, cancellationToken);

        this.logger.LogInformation(
            "Survey {SurveyId} created by author {AuthorId}",
            survey.Id,
            authorId);

        return Result<Guid>.Success(survey.Id);
    }

    public async Task<Result<IReadOnlyCollection<SurveyListItemDto>>> GetMySurveysAsync(
        Guid authorId,
        CancellationToken cancellationToken)
    {
        if (authorId == Guid.Empty)
        {
            return Result<IReadOnlyCollection<SurveyListItemDto>>.Failure("Invalid author id.");
        }

        var surveys = await this.surveyRepository.GetByAuthorIdAsync(authorId, cancellationToken);
        return Result<IReadOnlyCollection<SurveyListItemDto>>.Success(MapToList(surveys));
    }

    public async Task<Result<IReadOnlyCollection<SurveyListItemDto>>> GetPublicSurveysAsync(
        CancellationToken cancellationToken)
    {
        var surveys = await this.surveyRepository.GetPublicAsync(cancellationToken);
        return Result<IReadOnlyCollection<SurveyListItemDto>>.Success(MapToList(surveys));
    }

    private static IReadOnlyCollection<SurveyListItemDto> MapToList(IEnumerable<Survey> surveys)
    {
        return surveys
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SurveyListItemDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Status = s.Status,
                IsPublic = s.IsPublic,
                CreatedAt = s.CreatedAt,
            })
            .ToList();
    }
}
