// <copyright file="CreateQuestionRequestDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Questions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public sealed class CreateQuestionRequestDto
{
    public Guid SurveyId { get; set; }

    public string Text { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public List<string>? Options { get; set; }
}
