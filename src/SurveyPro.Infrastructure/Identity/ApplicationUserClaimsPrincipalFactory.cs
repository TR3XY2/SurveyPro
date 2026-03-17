// <copyright file="ApplicationUserClaimsPrincipalFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Infrastructure.Identity;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SurveyPro.Domain.Entities;

/// <summary>
/// Custom claims principal factory for application users.
/// Adds additional claims to the user's identity, such as the user's full name.
/// </summary>
public class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationUserClaimsPrincipalFactory"/> class.
    /// </summary>
    /// <param name="userManager">The user manager instance.</param>
    /// <param name="roleManager">The role manager instance.</param>
    /// <param name="options">The identity options.</param>
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    /// <summary>
    /// Generates a claims identity for the specified user.
    /// </summary>
    /// <param name="user">The user for whom to generate claims.</param>
    /// <returns>The claims identity.</returns>
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("FullName", user.Name));
        return identity;
    }
}