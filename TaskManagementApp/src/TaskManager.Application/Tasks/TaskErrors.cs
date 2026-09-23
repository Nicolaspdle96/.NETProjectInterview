using TaskManager.Application.Common.Results;

namespace TaskManager.Application.Tasks;

public static class TaskErrors
{
    /// <summary>Also returned for tasks owned by another user, so their existence is not revealed.</summary>
    public static readonly Error NotFound = Error.NotFound("Tasks.NotFound", "The task was not found.");
}
