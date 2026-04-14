// <copyright file="SurveyCsvExporterTests.cs" company="PlaceholderCompany">
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
/// Unit tests for <see cref="SurveyCsvExporter"/>.
/// </summary>
public class SurveyCsvExporterTests
{
    private static SurveyResponsesViewModel MakeViewModel(int responseCount = 1)
    {
        return new SurveyResponsesViewModel
        {
            SurveyId = Guid.NewGuid(),
            SurveyTitle = "Test Survey",
            SurveyDescription = "Test Description",
            AccessCode = "ABCD1234",
            TotalSubmittedResponses = responseCount,
            Responses = Enumerable.Range(0, responseCount).Select(i => new SurveyResponseViewModel
            {
                ResponseId = Guid.NewGuid(),
                RespondentName = $"User {i + 1}",
                RespondentEmail = $"user{i + 1}@example.com",
                SubmittedAt = DateTime.UtcNow.AddMinutes(-i),
                Answers = new List<SurveyResponseAnswerViewModel>
                {
                    new SurveyResponseAnswerViewModel
                    {
                        QuestionOrderNumber = 1,
                        QuestionText = "How are you?",
                        QuestionType = "Text",
                        TextAnswer = $"Fine, user {i + 1}",
                    },
                    new SurveyResponseAnswerViewModel
                    {
                        QuestionOrderNumber = 2,
                        QuestionText = "Pick a color",
                        QuestionType = "SingleChoice",
                        SelectedOptionTexts = new List<string> { "Blue" },
                    },
                },
            }).ToList(),
        };
    }

    [Fact]
    public void GenerateResponsesCsv_ReturnsNonEmptyByteArray()
    {
        var model = MakeViewModel();

        var result = SurveyCsvExporter.GenerateResponsesCsv(model);

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateResponsesCsv_OutputContainsRespondentName()
    {
        var model = MakeViewModel();

        var bytes = SurveyCsvExporter.GenerateResponsesCsv(model);
        var text = Encoding.UTF8.GetString(bytes);

        text.Should().Contain("User 1");
    }

    [Fact]
    public void GenerateResponsesCsv_OutputContainsRespondentEmail()
    {
        var model = MakeViewModel();

        var bytes = SurveyCsvExporter.GenerateResponsesCsv(model);
        var text = Encoding.UTF8.GetString(bytes);

        text.Should().Contain("user1@example.com");
    }

    [Fact]
    public void GenerateResponsesCsv_OutputContainsQuestionText()
    {
        var model = MakeViewModel();

        var bytes = SurveyCsvExporter.GenerateResponsesCsv(model);
        var text = Encoding.UTF8.GetString(bytes);

        text.Should().Contain("How are you?");
    }

    [Fact]
    public void GenerateResponsesCsv_OutputContainsTextAnswer()
    {
        var model = MakeViewModel();

        var bytes = SurveyCsvExporter.GenerateResponsesCsv(model);
        var text = Encoding.UTF8.GetString(bytes);

        text.Should().Contain("Fine, user 1");
    }

    [Fact]
    public void GenerateResponsesCsv_OutputContainsSelectedOptionText()
    {
        var model = MakeViewModel();

        var bytes = SurveyCsvExporter.GenerateResponsesCsv(model);
        var text = Encoding.UTF8.GetString(bytes);

        text.Should().Contain("Blue");
    }

    [Fact]
    public void GenerateResponsesCsv_MultipleResponses_OutputContainsAllRespondents()
    {
        var model = MakeViewModel(responseCount: 3);

        var bytes = SurveyCsvExporter.GenerateResponsesCsv(model);
        var text = Encoding.UTF8.GetString(bytes);

        text.Should().Contain("User 1");
        text.Should().Contain("User 2");
        text.Should().Contain("User 3");
    }

    [Fact]
    public void GenerateResponsesCsv_OutputContainsCsvHeader()
    {
        var model = MakeViewModel();

        var bytes = SurveyCsvExporter.GenerateResponsesCsv(model);
        var text = Encoding.UTF8.GetString(bytes);

        text.Should().Contain("Question,Answer");
    }

    [Fact]
    public void GenerateResponsesCsv_EmptyAnswerText_OutputContainsDash()
    {
        var model = new SurveyResponsesViewModel
        {
            SurveyTitle = "T",
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
                            QuestionText = "Unanswered?",
                            QuestionType = "Text",
                        },
                    },
                },
            },
        };

        var bytes = SurveyCsvExporter.GenerateResponsesCsv(model);
        var text = Encoding.UTF8.GetString(bytes);

        text.Should().Contain("\"-\"");
    }

    [Fact]
    public void GenerateResponsesCsv_AnswerWithDoubleQuotes_SanitizesQuotes()
    {
        var model = new SurveyResponsesViewModel
        {
            SurveyTitle = "T",
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
                            QuestionText = "Thoughts?",
                            QuestionType = "Text",
                            TextAnswer = "He said \"hello\"",
                        },
                    },
                },
            },
        };

        var bytes = SurveyCsvExporter.GenerateResponsesCsv(model);
        var text = Encoding.UTF8.GetString(bytes);

        text.Should().Contain("He said 'hello'");
    }
    
    [Fact]
    public void GenerateResponsesCsv_EmptyResponseList_ReturnsEmptyContent()
    {
        // Arrange
        var model = new SurveyResponsesViewModel
        {
            SurveyTitle = "Empty",
            Responses = new List<SurveyResponseViewModel>(),
        };

        var bytes = SurveyCsvExporter.GenerateResponsesCsv(model);
        
        var text = Encoding.UTF8.GetString(bytes).Replace("\uFEFF", "").Trim();

        text.Should().BeEmpty();
    }

    [Fact]
    public void GenerateResponsesCsv_MultipleOptions_JoinsWithCommaAndSpace()
    {
        var model = new SurveyResponsesViewModel
        {
            SurveyTitle = "T",
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
                            QuestionText = "Pick all",
                            QuestionType = "MultipleChoice",
                            SelectedOptionTexts = new List<string> { "Red", "Green", "Blue" },
                        },
                    },
                },
            },
        };

        var bytes = SurveyCsvExporter.GenerateResponsesCsv(model);
        var text = Encoding.UTF8.GetString(bytes);

        text.Should().Contain("Red, Green, Blue");
    }
}