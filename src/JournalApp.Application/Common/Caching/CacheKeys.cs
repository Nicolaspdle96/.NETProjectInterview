namespace JournalApp.Application.Common.Caching;

/// <summary>
/// Cache keys. Every key includes the user id, so a cached value can only be served back
/// to the user it was read for; this keeps the ownership rule intact.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Holds a token that is part of every entry key of the user. Removing it makes all of the
    /// user's cached entry reads unreachable at once (they then expire on their own).
    /// </summary>
    public static string EntriesVersion(Guid userId) => $"entries:{userId}:version";

    public static string EntryList(Guid userId, string version) => $"entries:{userId}:{version}:list";

    public static string Entry(Guid userId, string version, Guid entryId) => $"entries:{userId}:{version}:item:{entryId}";

    public static string CurrentUser(Guid userId) => $"users:{userId}";
}
