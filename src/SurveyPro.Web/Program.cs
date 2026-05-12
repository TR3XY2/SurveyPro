// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SurveyPro.Application.Configuration;
using SurveyPro.Application.Interfaces;
using SurveyPro.Domain.Entities;
using SurveyPro.Infrastructure.Identity;
using SurveyPro.Infrastructure.Interfaces;
using SurveyPro.Infrastructure.Persistence;
using SurveyPro.Infrastructure.Repositories;
using SurveyPro.Infrastructure.Services;
using SurveyPro.Web.Hubs;
using SurveyPro.Web.Infrastructure.Middleware;
using SurveyPro.Web.Infrastructure;
using SurveyPro.Web.Services;
using System.Threading.Tasks;

/// <summary>
/// Represents the main entry point for the SurveyPro web application.
/// </summary>
public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddUserSecrets<Program>();

        // Serilog
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "SurveyPro")
            .WriteTo.Console()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.Host.UseSerilog();

        Log.Information("SurveyPro application started");

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddSignalR();
        builder.Services.AddHostedService<NotificationDispatcherBackgroundService>();

        builder.Services.AddDbContext<SurveyProDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<SurveyProDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddMemoryCache();
        builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("Caching"));
        builder.Services.AddScoped<ISurveyRepository, SurveyRepository>();
        builder.Services.AddScoped<ISurveyService, SurveyService>();
        builder.Services.AddScoped<ISurveyParticipationService, SurveyParticipationService>();
        builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
        builder.Services.AddScoped<IQuestionService, QuestionService>();
        builder.Services.AddScoped<IChartService, ChartService>();
        builder.Services.AddScoped<IAdminUserService, AdminUserService>();
        builder.Services.AddScoped<IAdminSurveyService, AdminSurveyService>();
        builder.Services.AddHttpClient<SurveyPro.Application.Interfaces.IQuoteService, SurveyPro.Infrastructure.ExternalApis.QuoteService>(client =>
        {
            client.BaseAddress = new Uri(
                builder.Configuration["ExternalApis:QuoteApiBaseUrl"] ?? string.Empty);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
            };
            return handler;
        });

        var app = builder.Build();
        app.UseDeveloperExceptionPage();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SurveyProDbContext>();
            await db.Database.MigrateAsync();

            var roleManager =
                scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            await RoleSeeder.SeedRolesAsync(roleManager);
        }

        // Http request logging with Serilog
        app.UseSerilogRequestLogging();

        // Configure the HTTP request pipeline.
        app.UseExceptionHandler();

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseMiddleware<RequestExecutionTimeLoggingMiddleware>();
        app.UseMiddleware<RequestInfoLoggingMiddleware>();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.MapHub<NotificationHub>("/hubs/notifications");

        await app.RunAsync();
    }
}
