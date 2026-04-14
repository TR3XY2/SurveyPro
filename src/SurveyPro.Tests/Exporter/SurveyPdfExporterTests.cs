// <copyright file="SurveyPdfExporterTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Tests.Exporter;

using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using SurveyPro.Web.Services;
using SurveyPro.Web.ViewModels.Surveys;
using Xunit;


/// <summary>
/// Unit tests for <see cref="SurveyPdfExporter"/>.
/// </summary>
public class SurveyPdfExporterTests
{
    private static SurveyResponsesViewModel MakeViewModel(int responseCount = 1)
    {
        return new SurveyResponsesViewModel
        {
            SurveyId = Guid.NewGuid(),
            SurveyTitle = "Quarterly Review",
            SurveyDescription = "Q4 Review Survey",
            AccessCode = "XYZ123",
            TotalSubmittedResponses = responseCount,
            Responses = Enumerable.Range(0, responseCount).Select(i => new SurveyResponseViewModel
            {
                ResponseId = Guid.NewGuid(),
                RespondentName = $"Respondent {i + 1}",
                RespondentEmail = $"r{i + 1}@example.com",
                SubmittedAt = DateTime.UtcNow.AddMinutes(-i),
                Answers = new List<SurveyResponseAnswerViewModel>
                {
                    new SurveyResponseAnswerViewModel
                    {
                        QuestionOrderNumber = 1,
                        QuestionText = "Rate your experience",
                        QuestionType = "SingleChoice",
                        SelectedOptionTexts = new List<string> { "Excellent" },
                    },
                    new SurveyResponseAnswerViewModel
                    {
                        QuestionOrderNumber = 2,
                        QuestionText = "Additional comments",
                        QuestionType = "Text",
                        TextAnswer = "Great experience overall!",
                    },
                },
            }).ToList(),
        };
    }

    [Fact]
    public void GenerateResponsesPdf_ReturnsNonEmptyByteArray()
    {
        var model = MakeViewModel();

        var result = SurveyPdfExporter.GenerateResponsesPdf(model);

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateResponsesPdf_ReturnsByteArrayStartingWithPdfHeader()
    {
        var model = MakeViewModel();

        var bytes = SurveyPdfExporter.GenerateResponsesPdf(model);

        // PDF files start with "%PDF"
        var header = Encoding.ASCII.GetString(bytes, 0, 4);
        header.Should().Be("%PDF");
    }

    [Fact]
    public void GenerateResponsesPdf_WithNoResponses_ReturnsValidPdf()
    {
        var model = new SurveyResponsesViewModel
        {
            SurveyTitle = "Empty Survey",
            SurveyDescription = "No responses",
            AccessCode = "EMPTY1",
            TotalSubmittedResponses = 0,
            Responses = new List<SurveyResponseViewModel>(),
        };

        var result = SurveyPdfExporter.GenerateResponsesPdf(model);

        result.Should().NotBeNullOrEmpty();
        var header = Encoding.ASCII.GetString(result, 0, 4);
        header.Should().Be("%PDF");
    }

    [Fact]
    public void GenerateResponsesPdf_WithMultipleResponses_ReturnsValidPdf()
    {
        var model = MakeViewModel(responseCount: 5);

        var result = SurveyPdfExporter.GenerateResponsesPdf(model);

        result.Should().NotBeNullOrEmpty();
        var header = Encoding.ASCII.GetString(result, 0, 4);
        header.Should().Be("%PDF");
    }

    [Fact]
    public void GenerateResponsesPdf_WithTextAnswer_ReturnsValidPdf()
    {
        var model = new SurveyResponsesViewModel
        {
            SurveyTitle = "Text Survey",
            Responses = new List<SurveyResponseViewModel>
            {
                new SurveyResponseViewModel
                {
                    RespondentName = "Alice",
                    RespondentEmail = "alice@example.com",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<SurveyResponseAnswerViewModel>
                    {
                        new SurveyResponseAnswerViewModel
                        {
                            QuestionOrderNumber = 1,
                            QuestionText = "Describe your experience",
                            QuestionType = "Text",
                            TextAnswer = "It was wonderful.",
                        },
                    },
                },
            },
        };

        var result = SurveyPdfExporter.GenerateResponsesPdf(model);

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateResponsesPdf_WithEmptyAnswer_ReturnsValidPdf()
    {
        var model = new SurveyResponsesViewModel
        {
            SurveyTitle = "Partial Survey",
            Responses = new List<SurveyResponseViewModel>
            {
                new SurveyResponseViewModel
                {
                    RespondentName = "Bob",
                    RespondentEmail = "bob@example.com",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<SurveyResponseAnswerViewModel>
                    {
                        new SurveyResponseAnswerViewModel
                        {
                            QuestionOrderNumber = 1,
                            QuestionText = "Unanswered question",
                            QuestionType = "Text",
                            TextAnswer = null,
                        },
                    },
                },
            },
        };

        var act = () => SurveyPdfExporter.GenerateResponsesPdf(model);

        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateResponsesPdf_WithNullDescription_DoesNotThrow()
    {
        var model = new SurveyResponsesViewModel
        {
            SurveyTitle = "No Desc Survey",
            SurveyDescription = null,
            Responses = new List<SurveyResponseViewModel>(),
        };

        var act = () => SurveyPdfExporter.GenerateResponsesPdf(model);

        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateResponsesPdf_WithMultipleChoiceAnswer_ReturnsValidPdf()
    {
        var model = new SurveyResponsesViewModel
        {
            SurveyTitle = "Choice Survey",
            Responses = new List<SurveyResponseViewModel>
            {
                new SurveyResponseViewModel
                {
                    RespondentName = "Carol",
                    RespondentEmail = "carol@example.com",
                    SubmittedAt = DateTime.UtcNow,
                    Answers = new List<SurveyResponseAnswerViewModel>
                    {
                        new SurveyResponseAnswerViewModel
                        {
                            QuestionOrderNumber = 1,
                            QuestionText = "Select all that apply",
                            QuestionType = "MultipleChoice",
                            SelectedOptionTexts = new List<string> { "Option A", "Option C" },
                        },
                    },
                },
            },
        };

        var result = SurveyPdfExporter.GenerateResponsesPdf(model);

        result.Should().NotBeNullOrEmpty();
    }
}