// <copyright file="SurveyRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Infrastructure.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SurveyPro.Domain.Entities;
using SurveyPro.Domain.Enums;
using SurveyPro.Infrastructure.Interfaces;
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
            .Where(s => s.IsPublic && s.Status == SurveyStatuses.Published)
            .OrderByDescending(s => s.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Survey?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await this.dbContext.Surveys
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Survey survey, CancellationToken cancellationToken)
    {
        this.dbContext.Surveys.Update(survey);
        await this.dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid surveyId, CancellationToken cancellationToken)
{
    var survey = await this.dbContext.Surveys
        .FirstOrDefaultAsync(s => s.Id == surveyId, cancellationToken);

    if (survey != null)
    {
        this.dbContext.Surveys.Remove(survey);
        await this.dbContext.SaveChangesAsync(cancellationToken);
    }
}
}
