using TaskManager.Application.Auth;
using TaskManager.Domain.Users;

namespace TaskManager.Application.Tests.Auth;

public sealed class AuthValidatorTests
{
    private readonly RegisterRequestValidator _registerValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();

    [Fact]
    public void RegisterValidator_ValidRequest_Passes()
    {
        _registerValidator.Validate(new RegisterRequest("alice@example.com", "Passw0rd")).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    public void RegisterValidator_InvalidEmail_FailsOnEmail(string email)
    {
        var result = _registerValidator.Validate(new RegisterRequest(email, "Passw0rd"));

        result.Errors.ShouldContain(failure => failure.PropertyName == nameof(RegisterRequest.Email));
    }

    [Fact]
    public void RegisterValidator_EmailOverMaxLength_FailsOnEmail()
    {
        var email = new string('a', User.EmailMaxLength) + "@example.com";

        var result = _registerValidator.Validate(new RegisterRequest(email, "Passw0rd"));

        result.Errors.ShouldContain(failure => failure.PropertyName == nameof(RegisterRequest.Email));
    }

    [Theory]
    [InlineData("", "empty")]
    [InlineData("Pass0rd", "7 characters")]
    [InlineData("password", "no digit")]
    [InlineData("12345678", "no letter")]
    public void RegisterValidator_WeakPassword_FailsOnPassword(string password, string reason)
    {
        var result = _registerValidator.Validate(new RegisterRequest("alice@example.com", password));

        result.Errors.ShouldContain(failure => failure.PropertyName == nameof(RegisterRequest.Password), reason);
    }

    [Theory]
    [InlineData("abcdefg1")]
    [InlineData("1234567a")]
    [InlineData("long passphrase 42")]
    public void RegisterValidator_PasswordMeetingPolicy_Passes(string password)
    {
        _registerValidator.Validate(new RegisterRequest("alice@example.com", password)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void LoginValidator_AnyNonEmptyCredentials_Passes()
    {
        _loginValidator.Validate(new LoginRequest("whatever", "x")).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "x")]
    [InlineData("alice@example.com", "")]
    public void LoginValidator_MissingField_Fails(string email, string password)
    {
        _loginValidator.Validate(new LoginRequest(email, password)).IsValid.ShouldBeFalse();
    }
}
