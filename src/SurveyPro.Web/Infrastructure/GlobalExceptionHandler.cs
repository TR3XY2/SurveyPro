// <copyright file="GlobalExceptionHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.Infrastructure;

using Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// Logs unhandled exceptions and redirects to a safe page.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        this.logger = logger;
    }

    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        this.logger.LogError(
            exception,
            "Unhandled exception for path {Path}. TraceId: {TraceId}",
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        httpContext.Response.Redirect("/Home/Error");
        return ValueTask.FromResult(true);
    }
}
