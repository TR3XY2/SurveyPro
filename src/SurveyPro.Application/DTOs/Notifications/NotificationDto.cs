// <copyright file="NotificationDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Notifications;

using System;
using SurveyPro.Domain.Enums;

public sealed class NotificationDto
{
    public Guid Id { get; set; }

    public Guid RecipientUserId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public Guid? RelatedEntityId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DispatchedAt { get; set; }
}