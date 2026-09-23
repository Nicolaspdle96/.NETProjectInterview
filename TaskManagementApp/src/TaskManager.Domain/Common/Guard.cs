namespace TaskManager.Domain.Common;

internal static class Guard
{
    public static void AgainstNonUtc(DateTime value, string name)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new DomainException($"{name} must be expressed in UTC.");
        }
    }
}
