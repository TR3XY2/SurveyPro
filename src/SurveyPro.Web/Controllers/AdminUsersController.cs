// <copyright file="AdminUsersController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveyPro.Application.Interfaces;

/// <summary>
/// User management page for administrators.
/// Currently supports viewing users list.
/// </summary>
[Authorize(Roles = "Admin")]
public sealed class AdminUsersController : Controller
{
    private readonly IAdminUserService adminUserService;
    private readonly ILogger<AdminUsersController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminUsersController"/> class.
    /// </summary>
    /// <param name="adminUserService">Admin users service.</param>
    /// <param name="logger">Logger instance.</param>
    public AdminUsersController(
        IAdminUserService adminUserService,
        ILogger<AdminUsersController> logger)
    {
        this.adminUserService = adminUserService;
        this.logger = logger;
    }

    /// <summary>
    /// Displays users list for administrators.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Users list view.</returns>
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await this.adminUserService.GetUsersAsync(cancellationToken);

        this.logger.LogInformation("Admin opened users management page. Users count: {Count}", users.Count);
        return this.View(users);
    }

    [HttpPost]
    public async Task<IActionResult> Block(string id, CancellationToken ct)
    {
        await adminUserService.BlockUserAsync(id, ct);
        return RedirectToAction("Index");
    }
}
