// <copyright file="ParticipationQuestionViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.ViewModels.Participation;

public sealed class ParticipationQuestionViewModel
{
    public Guid QuestionId { get; set; }

    public string Text { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int OrderNumber { get; set; }

    public List<ParticipationOptionViewModel> Options { get; set; } = new ();

    public string? TextAnswer { get; set; }

    public Guid? SelectedOptionId { get; set; }

    public List<Guid> SelectedOptionIds { get; set; } = new ();
}
