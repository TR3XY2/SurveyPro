// <copyright file="RateLimitAttribute.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.Infrastructure.Filters;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Attribute wrapper for the custom rate limiting action filter.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RateLimitAttribute : TypeFilterAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitAttribute"/> class.
    /// </summary>
    /// <param name="maxRequestsPerMinute">Maximum requests allowed per minute.</param>
    public RateLimitAttribute(int maxRequestsPerMinute = 15)
        : base(typeof(RateLimitActionFilter))
    {
        this.Arguments = new object[] { maxRequestsPerMinute };
    }
}
