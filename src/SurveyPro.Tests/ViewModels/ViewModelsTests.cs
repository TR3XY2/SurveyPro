// <copyright file="ViewModelsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Tests.ViewModels;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using SurveyPro.Web.ViewModels;
using Xunit;

/// <summary>
/// Unit tests for ViewModel validation attributes.
/// </summary>
public class ViewModelValidationTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, results, true);
        return results;
    }

    [Fact]
    public void RegisterViewModel_ValidData_ShouldPassValidation()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "Password123!",
        };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void RegisterViewModel_EmptyName_ShouldFailValidation()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Name = string.Empty,
            Email = "john@example.com",
            Password = "Password123!",
        };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.Name)));
    }

    [Fact]
    public void RegisterViewModel_InvalidEmail_ShouldFailValidation()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Name = "John",
            Email = "not-an-email",
            Password = "Password123!",
        };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.Email)));
    }

    [Fact]
    public void RegisterViewModel_EmptyPassword_ShouldFailValidation()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Name = "John",
            Email = "john@example.com",
            Password = string.Empty,
        };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.Password)));
    }

    [Fact]
    public void LoginViewModel_ValidData_ShouldPassValidation()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "user@example.com",
            Password = "Password123!",
            RememberMe = false,
        };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void LoginViewModel_InvalidEmail_ShouldFailValidation()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "invalid",
            Password = "Password123!",
        };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.Email)));
    }

    [Fact]
    public void LoginViewModel_EmptyPassword_ShouldFailValidation()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "user@example.com",
            Password = string.Empty,
        };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.Password)));
    }

    [Fact]
    public void ChangePasswordViewModel_ValidData_ShouldPassValidation()
    {
        // Arrange
        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "NewPass1!",
            ConfirmNewPassword = "NewPass1!",
        };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ChangePasswordViewModel_EmptyCurrentPassword_ShouldFailValidation()
    {
        // Arrange
        var model = new ChangePasswordViewModel
        {
            CurrentPassword = string.Empty,
            NewPassword = "NewPass1!",
            ConfirmNewPassword = "NewPass1!",
        };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.CurrentPassword)));
    }

    [Fact]
    public void ChangePasswordViewModel_EmptyNewPassword_ShouldFailValidation()
    {
        // Arrange
        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "OldPass1!",
            NewPassword = string.Empty,
            ConfirmNewPassword = string.Empty,
        };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.NewPassword)));
    }

    [Fact]
    public void ChangePasswordViewModel_MismatchedPasswords_ShouldFailValidation()
    {
        // Arrange
        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "NewPass1!",
            ConfirmNewPassword = "DifferentPass1!",
        };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.ConfirmNewPassword)));
    }

    [Fact]
    public void EditProfileViewModel_ValidData_ShouldPassValidation()
    {
        // Arrange
        var model = new EditProfileViewModel
        {
            Name = "Jane",
            Email = "jane@example.com",
        };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void EditProfileViewModel_InvalidEmail_ShouldFailValidation()
    {
        // Arrange
        var model = new EditProfileViewModel
        {
            Name = "Jane",
            Email = "bad-email",
        };

        // Act
        var errors = Validate(model);

        // Assert
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.Email)));
    }

    [Fact]
    public void ProfileViewModel_ShouldHoldData()
    {
        // Arrange
        var model = new ProfileViewModel
        {
            Name = "Alice",
            Email = "alice@example.com",
        };

        // Act & Assert
        model.Name.Should().Be("Alice");
        model.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public void ProfileViewModel_DefaultValues_ShouldBeEmpty()
    {
        // Arrange
        var model = new ProfileViewModel();

        // Act & Assert
        model.Name.Should().Be(string.Empty);
        model.Email.Should().Be(string.Empty);
    }
}