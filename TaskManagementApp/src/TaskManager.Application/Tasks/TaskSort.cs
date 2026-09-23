using TaskManager.Application.Abstractions.Persistence;

namespace TaskManager.Application.Tasks;

/// <summary>Parses the <c>sort</c> query value: a field name, optionally prefixed with <c>-</c> for descending.</summary>
public static class TaskSort
{
    public const string AllowedValues = "created_at, -created_at, due_date, -due_date";

    public static readonly (TaskSortField Field, bool Descending) Default = (TaskSortField.CreatedAt, true);

    public static bool TryParse(string? value, out (TaskSortField Field, bool Descending) sort)
    {
        sort = Default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim();
        var descending = trimmed.StartsWith('-');
        var name = descending ? trimmed[1..] : trimmed;

        TaskSortField? field = name.ToLowerInvariant() switch
        {
            "created_at" => TaskSortField.CreatedAt,
            "due_date" => TaskSortField.DueDate,
            _ => null,
        };

        if (field is null)
        {
            return false;
        }

        sort = (field.Value, descending);
        return true;
    }
}
