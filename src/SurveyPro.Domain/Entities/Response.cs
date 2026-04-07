// <copyright file="Response.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Domain.Entities;

using System;

public class Response
{
    public Guid Id { get; set; }

    public Guid SessionParticipantId { get; set; }

    public SessionParticipant SessionParticipant { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDraft { get; set; }

    public ICollection<ResponseAnswer> Answers { get; set; } = new List<ResponseAnswer>();

    public DateTime? SubmittedAt { get; set; }
}