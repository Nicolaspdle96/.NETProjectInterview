namespace TaskManager.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
