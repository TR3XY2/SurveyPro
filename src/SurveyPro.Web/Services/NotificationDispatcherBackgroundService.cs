// <copyright file="NotificationDispatcherBackgroundService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.Services;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SurveyPro.Application.DTOs.Notifications;
using SurveyPro.Domain.Entities;
using SurveyPro.Infrastructure.Persistence;
using SurveyPro.Web.Hubs;

public sealed class NotificationDispatcherBackgroundService : BackgroundService
{
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IHubContext<NotificationHub> hubContext;
    private readonly ILogger<NotificationDispatcherBackgroundService> logger;

    public NotificationDispatcherBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationDispatcherBackgroundService> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.hubContext = hubContext;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await this.DispatchPendingNotificationsAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception, "Failed to dispatch notifications.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }

    private async Task DispatchPendingNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = this.serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SurveyProDbContext>();

        var pendingNotifications = await dbContext.Notifications
            .AsTracking()
            .Where(notification => notification.DispatchedAt == null)
            .OrderBy(notification => notification.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingNotifications.Count == 0)
        {
            return;
        }

        var dispatchedAt = DateTime.UtcNow;

        foreach (var notification in pendingNotifications)
        {
            await this.hubContext.Clients.User(notification.RecipientUserId.ToString())
                .SendAsync("notificationReceived", this.Map(notification), cancellationToken);

            notification.DispatchedAt = dispatchedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private NotificationDto Map(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            RecipientUserId = notification.RecipientUserId,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            RelatedEntityId = notification.RelatedEntityId,
            CreatedAt = notification.CreatedAt,
            DispatchedAt = notification.DispatchedAt,
        };
    }
}