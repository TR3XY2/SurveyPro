// <copyright file="IAdminUserService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.Interfaces;

using SurveyPro.Application.DTOs.Users;

/// <summary>
/// Provides read-only use cases for admin user management.
/// </summary>
public interface IAdminUserService
{
    /// <summary>
    /// Returns user list for admin page.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User list.</returns>
    Task<IReadOnlyCollection<AdminUserListItemDto>> GetUsersAsync(CancellationToken cancellationToken);

    Task BlockUserAsync(string userId, CancellationToken cancellationToken);
}