// <copyright file="SurveyService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SurveyPro.Application.Common;
using SurveyPro.Application.DTOs.Surveys;
using SurveyPro.Application.Interfaces;
using SurveyPro.Domain.Entities;
using SurveyPro.Domain.Enums;
using SurveyPro.Infrastructure.Interfaces;
using SurveyPro.Infrastructure.Persistence;
using System.Security.Cryptography;

/// <summary>
/// Survey use-case service.
/// </summary>
public sealed class SurveyService : ISurveyService
{
    private readonly ISurveyRepository surveyRepository;
    private readonly ILogger<SurveyService> logger;
    private readonly SurveyProDbContext? dbContext;

    public SurveyService(
        ISurveyRepository surveyRepository,
        ILogger<SurveyService> logger,
        SurveyProDbContext? dbContext = null)
    {
        this.surveyRepository = surveyRepository;
        this.logger = logger;
        this.dbContext = dbContext;
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

        if (this.dbContext != null)
        {
            await this.EnsureSurveySessionAsync(survey, cancellationToken);
        }

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
        return Result<IReadOnlyCollection<SurveyListItemDto>>.Success(
            await this.MapToListAsync(surveys, cancellationToken));
    }

    public async Task<Result<IReadOnlyCollection<SurveyListItemDto>>> GetPublicSurveysAsync(
        CancellationToken cancellationToken)
    {
        var surveys = await surveyRepository.GetPublicAsync(cancellationToken);
        return Result<IReadOnlyCollection<SurveyListItemDto>>.Success(
            await this.MapToListAsync(surveys, cancellationToken));
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

        var accessCode = string.Empty;
        if (this.dbContext != null)
        {
            accessCode = await this.GetAccessCodeAsync(survey.Id, cancellationToken);
        }

        return Result<SurveyListItemDto>.Success(new SurveyListItemDto
        {
            Id = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            Status = survey.Status,
            IsPublic = survey.IsPublic,
            CreatedAt = survey.CreatedAt,
            AccessCode = accessCode,
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

    private async Task<IReadOnlyCollection<SurveyListItemDto>> MapToListAsync(
        IEnumerable<Survey> surveys,
        CancellationToken cancellationToken)
    {
        var surveyList = surveys.ToList();
        var accessCodes = await this.LoadAccessCodesAsync(surveyList.Select(s => s.Id), cancellationToken);

        return surveyList
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SurveyListItemDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Status = s.Status,
                IsPublic = s.IsPublic,
                CreatedAt = s.CreatedAt,
                AccessCode = accessCodes.TryGetValue(s.Id, out var code) ? code : string.Empty,
            })
            .ToList();
    }

    private async Task<string> GetAccessCodeAsync(Guid surveyId, CancellationToken cancellationToken)
    {
        if (this.dbContext == null)
        {
            return string.Empty;
        }

        var code = await this.dbContext.SurveySessions
            .Where(session => session.SurveyId == surveyId && session.IsActive)
            .Select(session => session.AccessCode)
            .FirstOrDefaultAsync(cancellationToken);

        return code ?? string.Empty;
    }

    private async Task<Dictionary<Guid, string>> LoadAccessCodesAsync(
        IEnumerable<Guid> surveyIds,
        CancellationToken cancellationToken)
    {
        if (this.dbContext == null)
        {
            return new Dictionary<Guid, string>();
        }

        var ids = surveyIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var sessions = await this.dbContext.SurveySessions
            .AsNoTracking()
            .Where(session => ids.Contains(session.SurveyId) && session.IsActive)
            .ToListAsync(cancellationToken);

        return sessions
            .GroupBy(session => session.SurveyId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(session => session.CreatedAt)
                    .Select(session => session.AccessCode)
                    .FirstOrDefault() ?? string.Empty);
    }

    private async Task EnsureSurveySessionAsync(Survey survey, CancellationToken cancellationToken)
    {
        if (this.dbContext == null)
        {
            return;
        }

        var existingSession = await this.dbContext.SurveySessions
            .FirstOrDefaultAsync(session => session.SurveyId == survey.Id && session.IsActive, cancellationToken);

        if (existingSession != null)
        {
            return;
        }

        var accessCode = await this.GenerateUniqueAccessCodeAsync(cancellationToken);

        await this.dbContext.SurveySessions.AddAsync(
            new SurveySession
            {
                Id = Guid.NewGuid(),
                SurveyId = survey.Id,
                AccessCode = accessCode,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            },
            cancellationToken);

        await this.dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateUniqueAccessCodeAsync(CancellationToken cancellationToken)
    {
        if (this.dbContext == null)
        {
            return string.Empty;
        }

        while (true)
        {
            var code = this.GenerateAccessCode();

            var exists = await this.dbContext.SurveySessions
                .AnyAsync(session => session.AccessCode == code, cancellationToken);

            if (!exists)
            {
                return code;
            }
        }
    }

    private string GenerateAccessCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(8);

        var characters = new char[8];
        for (var i = 0; i < characters.Length; i++)
        {
            characters[i] = alphabet[bytes[i] % alphabet.Length];
        }

        return new string(characters);
    }
}
