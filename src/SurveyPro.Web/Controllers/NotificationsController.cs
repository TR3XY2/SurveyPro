// <copyright file="NotificationsController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyPro.Application.DTOs.Notifications;
using SurveyPro.Infrastructure.Persistence;

[Authorize]
public sealed class NotificationsController : BaseController
{
    private readonly SurveyProDbContext dbContext;

    public NotificationsController(SurveyProDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Recent(CancellationToken cancellationToken)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId.IsFailure)
        {
            return this.Unauthorized();
        }

        var notifications = await this.dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.RecipientUserId == currentUserId.Value)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(10)
            .Select(notification => new NotificationDto
            {
                Id = notification.Id,
                RecipientUserId = notification.RecipientUserId,
                Type = notification.Type,
                Title = notification.Title,
                Message = notification.Message,
                RelatedEntityId = notification.RelatedEntityId,
                CreatedAt = notification.CreatedAt,
                DispatchedAt = notification.DispatchedAt,
            })
            .ToListAsync(cancellationToken);

        return this.Json(notifications);
    }
}