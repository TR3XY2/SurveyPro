// <copyright file="NotificationType.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Domain.Enums;

public enum NotificationType
{
    /// <summary>
    /// Represents the event that occurs when a new survey is created.
    /// </summary>
    SurveyCreated,

    /// <summary>
    /// Represents the event that occurs when a survey has been updated.
    /// </summary>
    SurveyUpdated,

    /// <summary>
    /// Represents the event that occurs when a survey is published.
    /// </summary>
    SurveyPublished,

    /// <summary>
    /// Represents the event that occurs when a survey response has been submitted.
    /// </summary>
    SurveyResponseSubmitted,
}