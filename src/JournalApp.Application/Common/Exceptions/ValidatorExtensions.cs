using FluentValidation;
using JournalApp.Domain.Exceptions;

namespace JournalApp.Application.Common.Exceptions;

public static class ValidatorExtensions
{
    /// <summary>
    /// Validates the request and translates failures into a <see cref="DomainValidationException"/>
    /// so the API maps every validation error the same way.
    /// </summary>
    public static async Task EnsureValidAsync<T>(
        this IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        throw new DomainValidationException(errors);
    }
}
