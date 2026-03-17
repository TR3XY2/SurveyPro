// <copyright file="ProfileViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.ViewModels;

/// <summary>
/// Represents the profile information of a user.
/// Contains the user's name, email, and assigned roles.
/// </summary>
public class ProfileViewModel
{
    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}