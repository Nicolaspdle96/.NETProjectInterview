using FluentAssertions;
using JournalApp.Application.Auth.Dtos;
using JournalApp.Application.Auth.Validators;

namespace JournalApp.Application.Tests.Auth;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _sut = new();

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        // Act
        var result = _sut.Validate(new RegisterRequest("demo", "demo@journal.com", "Demo1234"));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("abcdefghijklmnopqrstuvwxyz12345")]
    public void Validate_InvalidUsernameLength_HasUsernameError(string username)
    {
        // Act
        var result = _sut.Validate(new RegisterRequest(username, "demo@journal.com", "Demo1234"));

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Username));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    public void Validate_InvalidEmail_HasEmailError(string email)
    {
        // Act
        var result = _sut.Validate(new RegisterRequest("demo", email, "Demo1234"));

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Email));
    }

    [Theory]
    [InlineData("Short1")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    [InlineData("")]
    public void Validate_WeakPassword_HasPasswordError(string password)
    {
        // Act
        var result = _sut.Validate(new RegisterRequest("demo", "demo@journal.com", password));

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Password));
    }
}
