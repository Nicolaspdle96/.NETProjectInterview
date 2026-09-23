using FluentValidation;
using JournalApp.Application.Entries.Dtos;
using JournalApp.Domain.Entities;
using JournalApp.Domain.Enums;

namespace JournalApp.Application.Entries.Validators;

public class CreateEntryRequestValidator : AbstractValidator<CreateEntryRequest>
{
    public CreateEntryRequestValidator()
    {
        this.AddEntryRules(r => r.Title, r => r.Content, r => r.Mood);
    }
}

public class UpdateEntryRequestValidator : AbstractValidator<UpdateEntryRequest>
{
    public UpdateEntryRequestValidator()
    {
        this.AddEntryRules(r => r.Title, r => r.Content, r => r.Mood);
    }
}

internal static class EntryValidationRules
{
    public static void AddEntryRules<T>(
        this AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, string>> title,
        System.Linq.Expressions.Expression<Func<T, string>> content,
        System.Linq.Expressions.Expression<Func<T, Mood?>> mood)
    {
        validator.RuleFor(title)
            .Cascade(CascadeMode.Stop)
            .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Title is required.")
            .Must(t => t.Trim().Length <= JournalEntry.TitleMaxLength)
            .WithMessage($"Title must be at most {JournalEntry.TitleMaxLength} characters.");

        validator.RuleFor(content)
            .Cascade(CascadeMode.Stop)
            .Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage("Content is required.")
            .MaximumLength(JournalEntry.ContentMaxLength)
            .WithMessage($"Content must be at most {JournalEntry.ContentMaxLength} characters.");

        validator.RuleFor(mood)
            .IsInEnum().WithMessage("Mood is not a valid value.");
    }
}
