using FluentValidation;
using JournalApp.Application.Auth.Dtos;
using JournalApp.Domain.Entities;

namespace JournalApp.Application.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public const int PasswordMinLength = 8;

    public RegisterRequestValidator()
    {
        RuleFor(r => r.Username)
            .Cascade(CascadeMode.Stop)
            .Must(u => !string.IsNullOrWhiteSpace(u)).WithMessage("Username is required.")
            .Must(u => u.Trim().Length is >= User.UsernameMinLength and <= User.UsernameMaxLength)
            .WithMessage($"Username must be between {User.UsernameMinLength} and {User.UsernameMaxLength} characters.");

        RuleFor(r => r.Email)
            .Cascade(CascadeMode.Stop)
            .Must(e => !string.IsNullOrWhiteSpace(e)).WithMessage("Email is required.")
            .Must(e => User.IsValidEmail(User.NormalizeEmail(e))).WithMessage("Email is not a valid email address.");

        RuleFor(r => r.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordMinLength).WithMessage($"Password must be at least {PasswordMinLength} characters.")
            .Must(p => p.Any(char.IsUpper)).WithMessage("Password must contain at least one uppercase letter.")
            .Must(p => p.Any(char.IsLower)).WithMessage("Password must contain at least one lowercase letter.")
            .Must(p => p.Any(char.IsDigit)).WithMessage("Password must contain at least one digit.");
    }
}
