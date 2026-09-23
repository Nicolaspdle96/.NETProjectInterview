using TaskManager.Application.Abstractions;

namespace TaskManager.Api.IntegrationTests.Infrastructure;

internal sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
{
    public DateTime UtcNow { get; } = utcNow;
}
