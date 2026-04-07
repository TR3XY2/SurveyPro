// <copyright file="SurveyParticipationViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.ViewModels.Participation;

public sealed class SurveyParticipationViewModel
{
    public Guid SurveyId { get; set; }

    public string AccessCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPublic { get; set; }

    public List<ParticipationQuestionViewModel> Questions { get; set; } = new ();
}
