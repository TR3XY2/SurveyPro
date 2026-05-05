// <copyright file="SurveyProDbContext.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Infrastructure.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SurveyPro.Domain.Entities;

public class SurveyProDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public SurveyProDbContext(DbContextOptions<SurveyProDbContext> options)
    : base(options)
    {
    }

    public DbSet<Survey> Surveys { get; set; }

    public DbSet<Question> Questions { get; set; }

    public DbSet<AnswerOption> AnswerOptions { get; set; }

    public DbSet<ResponseAnswer> ResponseAnswers { get; set; }

    public DbSet<Response> Responses { get; set; }

    public DbSet<SessionParticipant> SessionParticipants { get; set; }

    public DbSet<SurveySession> SurveySessions { get; set; }

    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Survey>()
            .HasOne(s => s.Author)
            .WithMany(u => u.Surveys)
            .HasForeignKey(s => s.AuthorId);

        builder.Entity<Survey>()
            .Property(s => s.Status)
            .HasConversion<string>();

        builder.Entity<Question>()
            .HasOne(q => q.Survey)
            .WithMany(s => s.Questions)
            .HasForeignKey(q => q.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AnswerOption>()
            .HasOne(o => o.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SurveySession>()
            .HasOne(s => s.Survey)
            .WithMany(su => su.Sessions)
            .HasForeignKey(s => s.SurveyId);

        builder.Entity<SessionParticipant>()
            .HasOne(sp => sp.Session)
            .WithMany(s => s.Participants)
            .HasForeignKey(sp => sp.SessionId);

        builder.Entity<SessionParticipant>()
            .HasOne(sp => sp.User)
            .WithMany(u => u.SessionParticipants)
            .HasForeignKey(sp => sp.UserId);

        builder.Entity<Response>()
            .HasOne(r => r.SessionParticipant)
            .WithMany(sp => sp.Responses)
            .HasForeignKey(r => r.SessionParticipantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ResponseAnswer>()
            .HasOne(ra => ra.Response)
            .WithMany(r => r.Answers)
            .HasForeignKey(ra => ra.ResponseId);

        builder.Entity<ResponseAnswer>()
            .HasOne(ra => ra.Option)
            .WithMany(o => o.ResponseAnswers)
            .HasForeignKey(ra => ra.OptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ResponseAnswer>()
            .HasOne(ra => ra.Question)
            .WithMany()
            .HasForeignKey(ra => ra.QuestionId);

        builder.Entity<Notification>()
            .Property(notification => notification.Type)
            .HasConversion<string>();

        builder.Entity<Notification>()
            .HasIndex(notification => new { notification.RecipientUserId, notification.DispatchedAt, notification.CreatedAt });
    }
}
