// <copyright file="ChartService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SurveyPro.Application.Common;
using SurveyPro.Application.DTOs.Charts;
using SurveyPro.Application.Interfaces;
using SurveyPro.Infrastructure.Persistence;
using SurveyPro.Infrastructure.Interfaces;

/// <summary>
/// Service for generating chart and histogram data from survey responses.
/// </summary>
public sealed class ChartService : IChartService
{
    private const string TextQuestionErrorMessage = "Charts and histograms cannot be built for text answers";

    private readonly SurveyProDbContext? dbContext;
    private readonly ISurveyRepository surveyRepository;
    private readonly ILogger<ChartService> logger;

    public ChartService(
        SurveyProDbContext? dbContext,
        ISurveyRepository surveyRepository,
        ILogger<ChartService> logger)
    {
        this.dbContext = dbContext;
        this.surveyRepository = surveyRepository;
        this.logger = logger;
    }

    /// <summary>
    /// Generate chart data for all questions in a survey.
    /// </summary>
    public async Task<Result<AnswerChartsDto>> GetSurveyChartsAsync(
        Guid surveyId,
        Guid requestedByUserId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        if (surveyId == Guid.Empty)
        {
            return Result<AnswerChartsDto>.Failure("Invalid survey id.");
        }

        if (requestedByUserId == Guid.Empty)
        {
            return Result<AnswerChartsDto>.Failure("Invalid user id.");
        }

        if (this.dbContext == null)
        {
            return Result<AnswerChartsDto>.Failure("Charts are unavailable in the current environment.");
        }

        var survey = await this.surveyRepository.GetByIdAsync(surveyId, cancellationToken);
        if (survey == null)
        {
            return Result<AnswerChartsDto>.Failure("Survey not found.");
        }

        if (!isAdministrator && survey.AuthorId != requestedByUserId)
        {
            return Result<AnswerChartsDto>.Failure("Access denied.");
        }

        var submittedResponses = await this.dbContext.Responses
            .AsNoTracking()
            .Where(response => !response.IsDraft)
            .Where(response => response.SessionParticipant.Session.SurveyId == surveyId)
            .Include(response => response.Answers)
                .ThenInclude(answer => answer.Question)
            .Include(response => response.Answers)
                .ThenInclude(answer => answer.Option)
            .ToListAsync(cancellationToken);

        var questions = await this.dbContext.Questions
            .AsNoTracking()
            .Where(q => q.SurveyId == surveyId)
            .OrderBy(q => q.OrderNumber)
            .ToListAsync(cancellationToken);

        var charts = new List<ChartDataDto>();
        var histograms = new List<HistogramDataDto>();

        foreach (var question in questions)
        {
            var questionAnswers = submittedResponses
                .SelectMany(r => r.Answers)
                .Where(a => a.QuestionId == question.Id)
                .ToList();

            if (question.Type == "Text")
            {
                charts.Add(new ChartDataDto
                {
                    QuestionId = question.Id.ToString(),
                    QuestionText = question.Text,
                    QuestionType = question.Type,
                    QuestionOrderNumber = question.OrderNumber,
                    CanBeCharted = false,
                    ErrorMessage = TextQuestionErrorMessage,
                    Labels = Array.Empty<ChartDataPoint>(),
                });

                var textAnswers = questionAnswers
                    .Where(a => !string.IsNullOrWhiteSpace(a.TextAnswer))
                    .Select(a => a.TextAnswer!)
                    .ToList();

                histograms.Add(CreateTextHistogram(question.Id, question.Text, question.OrderNumber, textAnswers));
            }
            else
            {
                var optionCounts = questionAnswers
                    .Where(a => a.Option != null)
                    .GroupBy(a => a.Option!.Text)
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                var total = optionCounts.Sum(x => x.Count);

                charts.Add(new ChartDataDto
                {
                    QuestionId = question.Id.ToString(),
                    QuestionText = question.Text,
                    QuestionType = question.Type,
                    QuestionOrderNumber = question.OrderNumber,
                    CanBeCharted = optionCounts.Any(),
                    Labels = optionCounts
                        .Select(x => new ChartDataPoint
                        {
                            Label = x.Label,
                            Count = x.Count,
                            Percentage = total > 0 ? Math.Round((decimal)x.Count / total * 100, 2) : 0,
                        })
                        .ToList(),
                });

                histograms.Add(new HistogramDataDto
                {
                    QuestionId = question.Id.ToString(),
                    QuestionText = question.Text,
                    QuestionType = question.Type,
                    QuestionOrderNumber = question.OrderNumber,
                    CanBeCharted = optionCounts.Any(),
                    TotalResponses = total,
                    Buckets = optionCounts
                        .Select(x => new HistogramBucket
                        {
                            Label = x.Label,
                            Count = x.Count,
                            Percentage = total > 0 ? Math.Round((decimal)x.Count / total * 100, 2) : 0,
                        })
                        .ToList(),
                });
            }
        }

        return Result<AnswerChartsDto>.Success(new AnswerChartsDto
        {
            SurveyId = surveyId,
            SurveyTitle = survey.Title,
            TotalSubmittedResponses = submittedResponses.Count,
            Charts = charts,
            Histograms = histograms,
        });
    }

    /// <summary>
    /// Get histogram data for a specific question.
    /// </summary>
    public async Task<Result<HistogramDataDto>> GetQuestionHistogramAsync(
        Guid questionId,
        Guid surveyId,
        Guid requestedByUserId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        if (questionId == Guid.Empty)
        {
            return Result<HistogramDataDto>.Failure("Invalid question id.");
        }

        if (surveyId == Guid.Empty)
        {
            return Result<HistogramDataDto>.Failure("Invalid survey id.");
        }

        if (this.dbContext == null)
        {
            return Result<HistogramDataDto>.Failure("Charts are unavailable in the current environment.");
        }

        var survey = await this.surveyRepository.GetByIdAsync(surveyId, cancellationToken);
        if (survey == null)
        {
            return Result<HistogramDataDto>.Failure("Survey not found.");
        }

        if (!isAdministrator && survey.AuthorId != requestedByUserId)
        {
            return Result<HistogramDataDto>.Failure("Access denied.");
        }

        var question = await this.dbContext.Questions
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == questionId && q.SurveyId == surveyId, cancellationToken);

        if (question == null)
        {
            return Result<HistogramDataDto>.Failure("Question not found.");
        }

        var questionAnswers = await this.dbContext.ResponseAnswers
            .AsNoTracking()
            .Where(a => a.QuestionId == questionId)
            .Where(a => !a.Response.IsDraft)
            .Include(a => a.Option)
            .ToListAsync(cancellationToken);

        if (question.Type == "Text")
        {
            var textAnswers = questionAnswers
                .Where(a => !string.IsNullOrWhiteSpace(a.TextAnswer))
                .Select(a => a.TextAnswer!)
                .ToList();

            return Result<HistogramDataDto>.Success(
                CreateTextHistogram(question.Id, question.Text, question.OrderNumber, textAnswers));
        }

        var buckets = questionAnswers
            .Where(a => a.Option != null)
            .GroupBy(a => a.Option!.Text)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var total = buckets.Sum(x => x.Count);

        return Result<HistogramDataDto>.Success(new HistogramDataDto
        {
            QuestionId = question.Id.ToString(),
            QuestionText = question.Text,
            QuestionType = question.Type,
            QuestionOrderNumber = question.OrderNumber,
            CanBeCharted = buckets.Any(),
            TotalResponses = total,
            Buckets = buckets
                .Select(b => new HistogramBucket
                {
                    Label = b.Label,
                    Count = b.Count,
                    Percentage = total > 0 ? Math.Round((decimal)b.Count / total * 100, 2) : 0,
                })
                .ToList(),
        });
    }

    private static HistogramDataDto CreateTextHistogram(
        Guid questionId,
        string questionText,
        int questionOrder,
        IReadOnlyCollection<string> textAnswers)
    {
        var answerCounts = textAnswers
            .GroupBy(a => a)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10) // Limit to top 10 for text answers
            .ToList();

        var total = textAnswers.Count;

        return new HistogramDataDto
        {
            QuestionId = questionId.ToString(),
            QuestionText = questionText,
            QuestionType = "Text",
            QuestionOrderNumber = questionOrder,
            CanBeCharted = answerCounts.Any(),
            TotalResponses = total,
            Buckets = answerCounts
                .Select(x => new HistogramBucket
                {
                    Label = x.Label,
                    Count = x.Count,
                    Percentage = total > 0 ? Math.Round((decimal)x.Count / total * 100, 2) : 0,
                })
                .ToList(),
        };
    }
}