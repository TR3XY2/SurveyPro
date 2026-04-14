// <copyright file="SurveyCsvExporter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Reflection.Metadata;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SurveyPro.Web.ViewModels.Surveys;

namespace SurveyPro.Web.Services;

public static class SurveyCsvExporter
{
    public static byte[] GenerateResponsesCsv(SurveyResponsesViewModel model)
    {
        var sb = new StringBuilder();

        foreach (var response in model.Responses)
        {
            sb.AppendLine($"=== {response.RespondentName} ===");
            sb.AppendLine($"Email: {response.RespondentEmail}");
            sb.AppendLine($"Submitted: {response.SubmittedAt:g}");
            sb.AppendLine();

            sb.AppendLine("Question,Answer");

            foreach (var answer in response.Answers)
            {
                var answerText =
                    !string.IsNullOrWhiteSpace(answer.TextAnswer)
                        ? answer.TextAnswer
                        : answer.SelectedOptionTexts.Any()
                            ? string.Join(", ", answer.SelectedOptionTexts)
                            : "-";

                // чистка щоб CSV не ламався
                answerText = answerText.Replace("\"", "'").Replace("\n", " ");

                sb.AppendLine($"\"{answer.QuestionText}\",\"{answerText}\"");
            }

            sb.AppendLine();
            sb.AppendLine("====================================");
            sb.AppendLine();
        }

        return Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();
    }
}