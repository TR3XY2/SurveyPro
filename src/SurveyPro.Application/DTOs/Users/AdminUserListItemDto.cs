// <copyright file="AdminUserListItemDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.DTOs.Users;

/// <summary>
/// Represents a single user row in admin users list.
/// </summary>
public sealed class AdminUserListItemDto
{
    /// <summary>
    /// Gets or sets user identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets full name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets role names.
    /// </summary>
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets value indicating whether user is blocked.
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// Gets or sets account creation date (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
}