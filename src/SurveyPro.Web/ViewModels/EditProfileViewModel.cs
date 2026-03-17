// <copyright file="EditProfileViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.ViewModels;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents the model used to edit a user's profile.
/// Contains the user's name and email address.
/// </summary>
public class EditProfileViewModel
{
    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    [Required]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

     /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}