namespace SurveyPro.Tests.ViewModels;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using SurveyPro.Web.ViewModels;
using Xunit;

/// <summary>
/// Tests for real ViewModel validation attributes (production code).
/// </summary>
public class ViewModelValidationTests
{
    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, results, true);
        return results;
    }

    // ─── RegisterViewModel ───────────────────────────────────────────────────

    [Fact]
    public void RegisterViewModel_ValidData_ShouldPassValidation()
    {
        var model = new RegisterViewModel
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "Password123!",
        };

        var errors = Validate(model);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void RegisterViewModel_EmptyName_ShouldFailValidation()
    {
        var model = new RegisterViewModel
        {
            Name = string.Empty,
            Email = "john@example.com",
            Password = "Password123!",
        };

        var errors = Validate(model);

        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.Name)));
    }

    [Fact]
    public void RegisterViewModel_InvalidEmail_ShouldFailValidation()
    {
        var model = new RegisterViewModel
        {
            Name = "John",
            Email = "not-an-email",
            Password = "Password123!",
        };

        var errors = Validate(model);

        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.Email)));
    }

    [Fact]
    public void RegisterViewModel_EmptyPassword_ShouldFailValidation()
    {
        var model = new RegisterViewModel
        {
            Name = "John",
            Email = "john@example.com",
            Password = string.Empty,
        };

        var errors = Validate(model);

        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.Password)));
    }

    // ─── LoginViewModel ──────────────────────────────────────────────────────

    [Fact]
    public void LoginViewModel_ValidData_ShouldPassValidation()
    {
        var model = new LoginViewModel
        {
            Email = "user@example.com",
            Password = "Password123!",
            RememberMe = false,
        };

        var errors = Validate(model);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void LoginViewModel_InvalidEmail_ShouldFailValidation()
    {
        var model = new LoginViewModel
        {
            Email = "invalid",
            Password = "Password123!",
        };

        var errors = Validate(model);

        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.Email)));
    }

    [Fact]
    public void LoginViewModel_EmptyPassword_ShouldFailValidation()
    {
        var model = new LoginViewModel
        {
            Email = "user@example.com",
            Password = string.Empty,
        };

        var errors = Validate(model);

        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.Password)));
    }

    // ─── ChangePasswordViewModel ─────────────────────────────────────────────

    [Fact]
    public void ChangePasswordViewModel_ValidData_ShouldPassValidation()
    {
        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "NewPass1!",
            ConfirmNewPassword = "NewPass1!",
        };

        var errors = Validate(model);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ChangePasswordViewModel_MismatchedPasswords_ShouldFailValidation()
    {
        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "OldPass1!",
            NewPassword = "NewPass1!",
            ConfirmNewPassword = "DifferentPass1!",
        };

        var errors = Validate(model);

        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.ConfirmNewPassword)));
    }

    // ─── EditProfileViewModel ────────────────────────────────────────────────

    [Fact]
    public void EditProfileViewModel_ValidData_ShouldPassValidation()
    {
        var model = new EditProfileViewModel
        {
            Name = "Jane",
            Email = "jane@example.com",
        };

        var errors = Validate(model);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void EditProfileViewModel_InvalidEmail_ShouldFailValidation()
    {
        var model = new EditProfileViewModel
        {
            Name = "Jane",
            Email = "bad-email",
        };

        var errors = Validate(model);

        errors.Should().Contain(e => e.MemberNames.Contains(nameof(model.Email)));
    }

    // ─── ProfileViewModel ────────────────────────────────────────────────────

    [Fact]
    public void ProfileViewModel_ShouldHoldData()
    {
        var model = new ProfileViewModel
        {
            Name = "Alice",
            Email = "alice@example.com",
        };

        model.Name.Should().Be("Alice");
        model.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public void ProfileViewModel_DefaultValues_ShouldBeEmpty()
    {
        var model = new ProfileViewModel();

        model.Name.Should().Be(string.Empty);
        model.Email.Should().Be(string.Empty);
    }
}