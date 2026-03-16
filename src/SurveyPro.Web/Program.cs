// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Web;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SurveyPro.Domain.Entities;
using SurveyPro.Infrastructure.Identity;
using SurveyPro.Infrastructure.Persistence;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddUserSecrets<Program>();

        // ������������ Serilog
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "SurveyPro")
            .WriteTo.Console()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
            .WriteTo.Seq("http://localhost:5341")
            .CreateLogger();

        builder.Host.UseSerilog();

        Log.Information("SurveyPro application started");

        // Add services to the container.
        builder.Services.AddControllersWithViews();

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

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var roleManager =
                scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            await RoleSeeder.SeedRolesAsync(roleManager);

            var db = scope.ServiceProvider.GetRequiredService<SurveyProDbContext>();
            await db.Database.MigrateAsync();
        }

        // ��������� HTTP ������
        app.UseSerilogRequestLogging();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        await app.RunAsync();
    }
}
