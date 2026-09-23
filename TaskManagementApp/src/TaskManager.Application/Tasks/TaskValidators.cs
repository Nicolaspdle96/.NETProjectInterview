using FluentValidation;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(request => request.Title).ValidTitle();
        RuleFor(request => request.Description).ValidDescription();
        RuleFor(request => request.Status).IsInEnum();

        // Compared by calendar day so a task due "today" is accepted at any time of day.
        RuleFor(request => request.DueDate)
            .Must(dueDate => dueDate.AsUtc()!.Value.Date >= dateTimeProvider.UtcNow.Date)
            .When(request => request.DueDate.HasValue)
            .WithMessage("'Due Date' must not be in the past.");
    }
}

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(request => request.Title).ValidTitle();
        RuleFor(request => request.Description).ValidDescription();
        RuleFor(request => request.Status).NotNull().IsInEnum();
    }
}

public sealed class ListTasksRequestValidator : AbstractValidator<ListTasksRequest>
{
    public ListTasksRequestValidator()
    {
        RuleFor(request => request.Page).GreaterThanOrEqualTo(1);
        RuleFor(request => request.PageSize).InclusiveBetween(1, ListTasksRequest.MaxPageSize);
    }
}

internal static class TaskValidationRules
{
    public static IRuleBuilderOptions<T, string> ValidTitle<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(title => !string.IsNullOrWhiteSpace(title)).WithMessage("'Title' must not be empty.")
            .Must(title => title is null || title.Trim().Length <= TaskItem.TitleMaxLength)
            .WithMessage($"'Title' must be {TaskItem.TitleMaxLength} characters or fewer.");

    public static IRuleBuilderOptions<T, string?> ValidDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(description => description is null || description.Trim().Length <= TaskItem.DescriptionMaxLength)
            .WithMessage($"'Description' must be {TaskItem.DescriptionMaxLength} characters or fewer.");
}
