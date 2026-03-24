// <copyright file="SurveyService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.Services;

using Microsoft.Extensions.Logging;
using SurveyPro.Application.Common;
using SurveyPro.Application.DTOs.Surveys;
using SurveyPro.Application.Interfaces;
using SurveyPro.Domain.Entities;
using SurveyPro.Domain.Enums;
using SurveyPro.Infrastructure.Interfaces;

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
        CreateSurveyRequestDto request,
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
            Status = SurveyStatuses.Draft,
            IsPublic = request.IsPublic,
            CreatedAt = DateTime.UtcNow,
        };

        await surveyRepository.AddAsync(survey, cancellationToken);

        logger.LogInformation(
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

        var surveys = await surveyRepository.GetByAuthorIdAsync(authorId, cancellationToken);
        return Result<IReadOnlyCollection<SurveyListItemDto>>.Success(MapToList(surveys));
    }

    public async Task<Result<IReadOnlyCollection<SurveyListItemDto>>> GetPublicSurveysAsync(
        CancellationToken cancellationToken)
    {
        var surveys = await surveyRepository.GetPublicAsync(cancellationToken);
        return Result<IReadOnlyCollection<SurveyListItemDto>>.Success(MapToList(surveys));
    }

    public async Task<Result> PublishAsync(Guid surveyId, Guid authorId, CancellationToken cancellationToken)
    {
        var survey = await this.surveyRepository.GetByIdAsync(surveyId, cancellationToken);

        if (survey == null)
        {
            return Result.Failure("Survey not found");
        }

        if (survey.AuthorId != authorId)
        {
            return Result.Failure("Access denied");
        }

        survey.Status = SurveyStatuses.Published;

        await this.surveyRepository.UpdateAsync(survey, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(
    Guid surveyId,
    Guid authorId,
    CancellationToken cancellationToken)
    {
        var surveys = await this.surveyRepository.GetByAuthorIdAsync(authorId, cancellationToken);
        var survey = surveys.FirstOrDefault(s => s.Id == surveyId);

        if (survey == null)
        {
            return Result.Failure("Survey not found");
        }

        await this.surveyRepository.DeleteAsync(surveyId, cancellationToken);

        this.logger.LogInformation("Survey {SurveyId} deleted", surveyId);

        return Result.Success();
    }

    public async Task<Result<SurveyListItemDto>> GetByIdAsync(
        Guid surveyId,
        Guid authorId,
        CancellationToken cancellationToken)
    {
        var survey = await this.surveyRepository.GetByIdAsync(surveyId, cancellationToken);

        if (survey == null)
        {
            return Result<SurveyListItemDto>.Failure("Survey not found.");
        }

        if (survey.AuthorId != authorId)
        {
            return Result<SurveyListItemDto>.Failure("Access denied.");
        }

        return Result<SurveyListItemDto>.Success(new SurveyListItemDto
        {
            Id = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            Status = survey.Status,
            IsPublic = survey.IsPublic,
            CreatedAt = survey.CreatedAt,
        });
    }

    public async Task<Result> UpdateAsync(
        Guid surveyId,
        Guid authorId,
        UpdateSurveyRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result.Failure("Survey title is required.");
        }

        var survey = await this.surveyRepository.GetByIdAsync(surveyId, cancellationToken);

        if (survey == null)
        {
            return Result.Failure("Survey not found.");
        }

        if (survey.AuthorId != authorId)
        {
            return Result.Failure("Access denied.");
        }

        survey.Title = request.Title.Trim();
        survey.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        survey.IsPublic = request.IsPublic;

        await this.surveyRepository.UpdateAsync(survey, cancellationToken);

        this.logger.LogInformation("Survey {SurveyId} updated by author {AuthorId}", surveyId, authorId);

        return Result.Success();
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
