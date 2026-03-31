// <copyright file="QuestionRepository.cs" company="PlaceholderCompany">
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
using SurveyPro.Infrastructure.Interfaces;
using SurveyPro.Infrastructure.Persistence;

public class QuestionRepository : IQuestionRepository
{
    private readonly SurveyProDbContext dbContext;

    public QuestionRepository(SurveyProDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task AddAsync(Question question, CancellationToken cancellationToken)
    {
        await dbContext.Questions.AddAsync(question, cancellationToken);
    }

    public async Task AddOptionsAsync(IEnumerable<AnswerOption> options, CancellationToken cancellationToken)
    {
        await dbContext.AnswerOptions.AddRangeAsync(options, cancellationToken);
    }

    public async Task<Survey?> GetSurveyByIdAsync(Guid surveyId, CancellationToken cancellationToken)
    {
        return await dbContext.Surveys
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == surveyId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Question>> GetQuestionsBySurveyIdAsync(Guid surveyId, CancellationToken cancellationToken)
    {
        return await dbContext.Questions
            .Where(q => q.SurveyId == surveyId)
            .Include(q => q.Options)
            .OrderBy(q => q.OrderNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<Question?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Questions
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public void Remove(Question question)
    {
        dbContext.Questions.Remove(question);
    }
}
