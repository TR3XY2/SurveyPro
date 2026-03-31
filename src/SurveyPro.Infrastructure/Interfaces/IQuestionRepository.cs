// <copyright file="IQuestionRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Infrastructure.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SurveyPro.Domain.Entities;

public interface IQuestionRepository
{
    Task AddAsync(Question question, CancellationToken cancellationToken);

    Task AddOptionsAsync(IEnumerable<AnswerOption> options, CancellationToken cancellationToken);

    Task<Survey?> GetSurveyByIdAsync(Guid surveyId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<List<Question>> GetQuestionsBySurveyIdAsync(Guid surveyId, CancellationToken cancellationToken);
}
