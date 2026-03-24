// <copyright file="SurveyRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SurveyPro.Application.Surveys;
using SurveyPro.Domain.Entities;
using SurveyPro.Infrastructure.Persistence;

/// <summary>
/// EF Core survey repository.
/// </summary>
public sealed class SurveyRepository : ISurveyRepository
{
    private readonly SurveyProDbContext dbContext;

    public SurveyRepository(SurveyProDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task AddAsync(Survey survey, CancellationToken cancellationToken)
    {
        await this.dbContext.Surveys.AddAsync(survey, cancellationToken);
        await this.dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Survey>> GetByAuthorIdAsync(
        Guid authorId,
        CancellationToken cancellationToken)
    {
        return await this.dbContext.Surveys
            .Where(s => s.AuthorId == authorId)
            .OrderByDescending(s => s.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Survey>> GetPublicAsync(CancellationToken cancellationToken)
    {
        return await this.dbContext.Surveys
            .Where(s => s.IsPublic || s.Status == "Published")
            .OrderByDescending(s => s.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
