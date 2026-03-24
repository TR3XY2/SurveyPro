// <copyright file="SurveyServiceTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Tests.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SurveyPro.Application.DTOs.Surveys;
using SurveyPro.Application.Services;
using SurveyPro.Domain.Entities;
using SurveyPro.Domain.Enums;
using SurveyPro.Infrastructure.Interfaces;
using Xunit;

/// <summary>
/// Unit tests for <see cref="SurveyService"/>.
/// </summary>
public class SurveyServiceTests
{
    private static readonly Guid ValidAuthorId = Guid.NewGuid();
    private static readonly Guid ValidSurveyId = Guid.NewGuid();

    private static SurveyService BuildService(Mock<ISurveyRepository> repo)
    {
        var logger = new Mock<ILogger<SurveyService>>();
        return new SurveyService(repo.Object, logger.Object);
    }

    private static Survey MakeSurvey(Guid? id = null, Guid? authorId = null) => new Survey
    {
        Id = id ?? ValidSurveyId,
        AuthorId = authorId ?? ValidAuthorId,
        Title = "Test Survey",
        Description = "Test Description",
        Status = SurveyStatuses.Draft,
        IsPublic = false,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task CreateAsync_EmptyAuthorId_ReturnsFailure()
    {
        // Arrange
        var repo = new Mock<ISurveyRepository>();
        var service = BuildService(repo);
        var request = new CreateSurveyRequestDto { Title = "Title" };

        // Act
        var result = await service.CreateAsync(Guid.Empty, request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid author id.");
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_ReturnsFailure()
    {
        // Arrange
        var repo = new Mock<ISurveyRepository>();
        var service = BuildService(repo);
        var request = new CreateSurveyRequestDto { Title = "   " };

        // Act
        var result = await service.CreateAsync(ValidAuthorId, request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Survey title is required.");
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsSuccessWithNewId()
    {
        // Arrange
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = BuildService(repo);
        var request = new CreateSurveyRequestDto { Title = "My Survey", IsPublic = true };

        // Act
        var result = await service.CreateAsync(ValidAuthorId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_TrimsTitle()
    {
        // Arrange
        Survey? saved = null;
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()))
            .Callback<Survey, CancellationToken>((s, _) => saved = s)
            .Returns(Task.CompletedTask);

        var service = BuildService(repo);
        var request = new CreateSurveyRequestDto { Title = "  Trimmed Title  " };

        // Act
        await service.CreateAsync(ValidAuthorId, request, CancellationToken.None);

        // Assert
        saved!.Title.Should().Be("Trimmed Title");
    }

    [Fact]
    public async Task CreateAsync_WhitespaceDescription_SavesNullDescription()
    {
        // Arrange
        Survey? saved = null;
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()))
            .Callback<Survey, CancellationToken>((s, _) => saved = s)
            .Returns(Task.CompletedTask);

        var service = BuildService(repo);
        var request = new CreateSurveyRequestDto { Title = "Title", Description = "   " };

        // Act
        await service.CreateAsync(ValidAuthorId, request, CancellationToken.None);

        // Assert
        saved!.Description.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SavesCorrectAuthorIdAndStatus()
    {
        // Arrange
        Survey? saved = null;
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()))
            .Callback<Survey, CancellationToken>((s, _) => saved = s)
            .Returns(Task.CompletedTask);

        var service = BuildService(repo);
        var request = new CreateSurveyRequestDto { Title = "Title" };

        // Act
        await service.CreateAsync(ValidAuthorId, request, CancellationToken.None);

        // Assert
        saved!.AuthorId.Should().Be(ValidAuthorId);
        saved.Status.Should().Be(SurveyStatuses.Draft);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CallsRepositoryAddOnce()
    {
        // Arrange
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = BuildService(repo);
        var request = new CreateSurveyRequestDto { Title = "Title" };

        // Act
        await service.CreateAsync(ValidAuthorId, request, CancellationToken.None);

        // Assert
        repo.Verify(r => r.AddAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMySurveysAsync_EmptyAuthorId_ReturnsFailure()
    {
        // Arrange
        var repo = new Mock<ISurveyRepository>();
        var service = BuildService(repo);

        // Act
        var result = await service.GetMySurveysAsync(Guid.Empty, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid author id.");
    }

    [Fact]
    public async Task GetMySurveysAsync_ValidAuthorId_ReturnsMappedSurveys()
    {
        // Arrange
        var surveys = new List<Survey>
        {
            MakeSurvey(Guid.NewGuid()),
            MakeSurvey(Guid.NewGuid()),
        };

        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByAuthorIdAsync(ValidAuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(surveys);

        var service = BuildService(repo);

        // Act
        var result = await service.GetMySurveysAsync(ValidAuthorId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMySurveysAsync_ReturnsSurveysOrderedByCreatedAtDescending()
    {
        // Arrange
        var older = MakeSurvey(Guid.NewGuid());
        older.CreatedAt = DateTime.UtcNow.AddDays(-1);
        older.Title = "Older";

        var newer = MakeSurvey(Guid.NewGuid());
        newer.CreatedAt = DateTime.UtcNow;
        newer.Title = "Newer";

        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByAuthorIdAsync(ValidAuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Survey> { older, newer });

        var service = BuildService(repo);

        // Act
        var result = await service.GetMySurveysAsync(ValidAuthorId, CancellationToken.None);

        // Assert
        result.Value!.First().Title.Should().Be("Newer");
    }

    [Fact]
    public async Task GetPublicSurveysAsync_ReturnsOnlyPublicSurveys()
    {
        // Arrange
        var s1 = MakeSurvey(Guid.NewGuid());
        s1.IsPublic = true;

        var s2 = MakeSurvey(Guid.NewGuid());
        s2.IsPublic = true;

        var surveys = new List<Survey> { s1, s2 };

        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetPublicAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(surveys);

        var service = BuildService(repo);

        // Act
        var result = await service.GetPublicSurveysAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.All(s => s.IsPublic).Should().BeTrue();
    }

    [Fact]
    public async Task GetPublicSurveysAsync_EmptyList_ReturnsSuccessWithEmptyCollection()
    {
        // Arrange
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetPublicAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Survey>());

        var service = BuildService(repo);

        // Act
        var result = await service.GetPublicSurveysAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_SurveyNotFound_ReturnsFailure()
    {
        // Arrange
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByIdAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Survey?)null);

        var service = BuildService(repo);

        // Act
        var result = await service.GetByIdAsync(ValidSurveyId, ValidAuthorId, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Survey not found.");
    }

    [Fact]
    public async Task GetByIdAsync_WrongAuthor_ReturnsFailure()
    {
        // Arrange
        var survey = MakeSurvey();
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByIdAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);

        var service = BuildService(repo);

        // Act
        var result = await service.GetByIdAsync(ValidSurveyId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied.");
    }

    [Fact]
    public async Task GetByIdAsync_ValidRequest_ReturnsCorrectDto()
    {
        // Arrange
        var survey = MakeSurvey();
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByIdAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);

        var service = BuildService(repo);

        // Act
        var result = await service.GetByIdAsync(ValidSurveyId, ValidAuthorId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(ValidSurveyId);
        result.Value.Title.Should().Be("Test Survey");
        result.Value.Description.Should().Be("Test Description");
        result.Value.Status.Should().Be(SurveyStatuses.Draft);
    }

    [Fact]
    public async Task UpdateAsync_EmptyTitle_ReturnsFailure()
    {
        // Arrange
        var repo = new Mock<ISurveyRepository>();
        var service = BuildService(repo);
        var request = new UpdateSurveyRequestDto { Title = "  " };

        // Act
        var result = await service.UpdateAsync(ValidSurveyId, ValidAuthorId, request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Survey title is required.");
    }

    [Fact]
    public async Task UpdateAsync_SurveyNotFound_ReturnsFailure()
    {
        // Arrange
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByIdAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Survey?)null);

        var service = BuildService(repo);
        var request = new UpdateSurveyRequestDto { Title = "New Title" };

        // Act
        var result = await service.UpdateAsync(ValidSurveyId, ValidAuthorId, request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Survey not found.");
    }

    [Fact]
    public async Task UpdateAsync_WrongAuthor_ReturnsFailure()
    {
        // Arrange
        var survey = MakeSurvey();
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByIdAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);

        var service = BuildService(repo);
        var request = new UpdateSurveyRequestDto { Title = "New Title" };

        // Act
        var result = await service.UpdateAsync(ValidSurveyId, Guid.NewGuid(), request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied.");
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var survey = MakeSurvey();
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByIdAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        repo.Setup(r => r.UpdateAsync(survey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = BuildService(repo);
        var request = new UpdateSurveyRequestDto { Title = "Updated Title", IsPublic = true };

        // Act
        var result = await service.UpdateAsync(ValidSurveyId, ValidAuthorId, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesSurveyFields()
    {
        // Arrange
        var survey = MakeSurvey();
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByIdAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        repo.Setup(r => r.UpdateAsync(survey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = BuildService(repo);
        var request = new UpdateSurveyRequestDto
        {
            Title = "  New Title  ",
            Description = "New Desc",
            IsPublic = true,
        };

        // Act
        await service.UpdateAsync(ValidSurveyId, ValidAuthorId, request, CancellationToken.None);

        // Assert
        survey.Title.Should().Be("New Title");
        survey.Description.Should().Be("New Desc");
        survey.IsPublic.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WhitespaceDescription_SetsNullDescription()
    {
        // Arrange
        var survey = MakeSurvey();
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByIdAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        repo.Setup(r => r.UpdateAsync(survey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = BuildService(repo);
        var request = new UpdateSurveyRequestDto { Title = "Title", Description = "   " };

        // Act
        await service.UpdateAsync(ValidSurveyId, ValidAuthorId, request, CancellationToken.None);

        // Assert
        survey.Description.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_CallsRepositoryUpdateOnce()
    {
        // Arrange
        var survey = MakeSurvey();
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByIdAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = BuildService(repo);
        var request = new UpdateSurveyRequestDto { Title = "Title" };

        // Act
        await service.UpdateAsync(ValidSurveyId, ValidAuthorId, request, CancellationToken.None);

        // Assert
        repo.Verify(r => r.UpdateAsync(survey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_SurveyNotFound_ReturnsFailure()
    {
        // Arrange
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByAuthorIdAsync(ValidAuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Survey>());

        var service = BuildService(repo);

        // Act
        var result = await service.DeleteAsync(ValidSurveyId, ValidAuthorId, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Survey not found");
    }

    [Fact]
    public async Task DeleteAsync_SurveyBelongsToOtherAuthor_ReturnsFailure()
    {
        // Arrange
        var otherAuthorId = Guid.NewGuid();
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByAuthorIdAsync(otherAuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Survey>());

        var service = BuildService(repo);

        // Act
        var result = await service.DeleteAsync(ValidSurveyId, otherAuthorId, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Survey not found");
    }

    [Fact]
    public async Task DeleteAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var survey = MakeSurvey();
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByAuthorIdAsync(ValidAuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Survey> { survey });
        repo.Setup(r => r.DeleteAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = BuildService(repo);

        // Act
        var result = await service.DeleteAsync(ValidSurveyId, ValidAuthorId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ValidRequest_CallsRepositoryDeleteWithCorrectId()
    {
        // Arrange
        var survey = MakeSurvey();
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByAuthorIdAsync(ValidAuthorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Survey> { survey });
        repo.Setup(r => r.DeleteAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = BuildService(repo);

        // Act
        await service.DeleteAsync(ValidSurveyId, ValidAuthorId, CancellationToken.None);

        // Assert
        repo.Verify(r => r.DeleteAsync(ValidSurveyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_SurveyNotFound_ReturnsFailure()
    {
        // Arrange
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByIdAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Survey?)null);

        var service = BuildService(repo);

        // Act
        var result = await service.PublishAsync(ValidSurveyId, ValidAuthorId, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Survey not found");
    }

    [Fact]
    public async Task PublishAsync_WrongAuthor_ReturnsFailure()
    {
        // Arrange
        var survey = MakeSurvey();
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByIdAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);

        var service = BuildService(repo);

        // Act
        var result = await service.PublishAsync(ValidSurveyId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task PublishAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var survey = MakeSurvey();
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByIdAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        repo.Setup(r => r.UpdateAsync(survey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = BuildService(repo);

        // Act
        var result = await service.PublishAsync(ValidSurveyId, ValidAuthorId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PublishAsync_ValidRequest_SetsSurveyStatusToPublished()
    {
        // Arrange
        var survey = MakeSurvey();
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByIdAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        repo.Setup(r => r.UpdateAsync(survey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = BuildService(repo);

        // Act
        await service.PublishAsync(ValidSurveyId, ValidAuthorId, CancellationToken.None);

        // Assert
        survey.Status.Should().Be(SurveyStatuses.Published);
    }

    [Fact]
    public async Task PublishAsync_ValidRequest_CallsRepositoryUpdateOnce()
    {
        // Arrange
        var survey = MakeSurvey();
        var repo = new Mock<ISurveyRepository>();
        repo.Setup(r => r.GetByIdAsync(ValidSurveyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(survey);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Survey>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = BuildService(repo);

        // Act
        await service.PublishAsync(ValidSurveyId, ValidAuthorId, CancellationToken.None);

        // Assert
        repo.Verify(r => r.UpdateAsync(survey, It.IsAny<CancellationToken>()), Times.Once);
    }
}