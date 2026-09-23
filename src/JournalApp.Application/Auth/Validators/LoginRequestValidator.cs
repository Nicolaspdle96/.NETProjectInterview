using FluentValidation;
using JournalApp.Application.Auth.Dtos;

namespace JournalApp.Application.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().WithMessage("Email is required.");
        RuleFor(r => r.Password).NotEmpty().WithMessage("Password is required.");
    }
}
