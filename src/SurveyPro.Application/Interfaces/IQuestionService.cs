// <copyright file="IQuestionService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SurveyPro.Application.Common;
using SurveyPro.Application.DTOs.Questions;

public interface IQuestionService
{
    Task<Result<Guid>> CreateAsync(
        Guid authorId,
        CreateQuestionRequestDto request,
        CancellationToken cancellationToken);
}
