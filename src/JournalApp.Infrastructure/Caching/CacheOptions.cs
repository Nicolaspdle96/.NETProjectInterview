namespace JournalApp.Infrastructure.Caching;

public class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>How long a cached read lives before it is reloaded from the database.</summary>
    public int ExpirationSeconds { get; set; } = 300;

    /// <summary>Maximum number of cached items; when full, older items are evicted.</summary>
    public int SizeLimit { get; set; } = 10_000;
}
