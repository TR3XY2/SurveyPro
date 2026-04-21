// <copyright file="AdminUserService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveyPro.Application.Configuration;
using SurveyPro.Application.DTOs.Users;
using SurveyPro.Application.Interfaces;
using SurveyPro.Domain.Entities;

/// <summary>
/// Reads admin user list and caches it in memory.
/// </summary>
public sealed class AdminUserService : IAdminUserService
{
    private const string UsersCacheKey = "admin.users.list";
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IMemoryCache memoryCache;
    private readonly CacheSettings cacheSettings;
    private readonly ILogger<AdminUserService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminUserService"/> class.
    /// </summary>
    /// <param name="userManager">Identity user manager.</param>
    /// <param name="memoryCache">In-memory cache.</param>
    /// <param name="cacheOptions">Cache configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public AdminUserService(
        UserManager<ApplicationUser> userManager,
        IMemoryCache memoryCache,
        IOptions<CacheSettings> cacheOptions,
        ILogger<AdminUserService> logger)
    {
        this.userManager = userManager;
        this.memoryCache = memoryCache;
        this.cacheSettings = cacheOptions.Value;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<AdminUserListItemDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        if (this.memoryCache.TryGetValue<IReadOnlyCollection<AdminUserListItemDto>>(UsersCacheKey, out var cachedUsers)
            && cachedUsers is not null)
        {
            this.logger.LogInformation("Admin users list was returned from cache");
            return cachedUsers;
        }

        var users = await this.userManager.Users
            .AsNoTracking()
            .OrderByDescending(user => user.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<AdminUserListItemDto>(users.Count);

        foreach (var user in users)
        {
            var roles = await this.userManager.GetRolesAsync(user);

            result.Add(new AdminUserListItemDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email ?? string.Empty,
                IsBlocked = user.IsBlocked,
                CreatedAt = user.CreatedAt,
                Roles = roles.OrderBy(role => role).ToArray(),
            });
        }

        var expirationMinutes = Math.Max(1, this.cacheSettings.UsersListExpirationMinutes);

        this.memoryCache.Set(
            UsersCacheKey,
            result,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes),
            });

        this.logger.LogInformation(
            "Admin users list was loaded from database and cached for {Minutes} minutes",
            expirationMinutes);

        return result;
    }

    public async Task BlockUserAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await this.userManager.FindByIdAsync(userId);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        user.IsBlocked = true;

        var result = await this.userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Failed to block user");
        }

        this.memoryCache.Remove(UsersCacheKey);

        this.logger.LogInformation("User {UserId} was blocked", userId);
    }
}