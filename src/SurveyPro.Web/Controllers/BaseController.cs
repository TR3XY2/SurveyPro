// <copyright file="BaseController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public abstract class BaseController : Controller
{
    protected (bool IsFailure, Guid Value) GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var parsed))
        {
            return (true, Guid.Empty);
        }

        return (false, parsed);
    }
}
