using FluentValidation;
using TaskManager.Domain.Users;

namespace TaskManager.Application.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public const int PasswordMinLength = 8;

    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .MaximumLength(User.EmailMaxLength)
            .EmailAddress();

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(PasswordMinLength)
            .Matches("[A-Za-z]").WithMessage("'{PropertyName}' must contain at least one letter.")
            .Matches("[0-9]").WithMessage("'{PropertyName}' must contain at least one digit.");
    }
}

/// <summary>
/// Only checks presence: applying the password policy here would tell a caller
/// something about which credentials could exist.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty();
        RuleFor(request => request.Password).NotEmpty();
    }
}
