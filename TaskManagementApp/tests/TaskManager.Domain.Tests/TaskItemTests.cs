using TaskManager.Domain.Common;
using TaskManager.Domain.Tasks;

namespace TaskManager.Domain.Tests;

public sealed class TaskItemTests
{
    private static readonly DateTime Now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.NewGuid();

    private static TaskItem CreateValid(
        string title = "Write report",
        string? description = "Quarterly numbers",
        TaskStatus status = TaskStatus.Todo,
        DateTime? dueDate = null) =>
        TaskItem.Create(UserId, title, description, status, dueDate, Now);

    [Fact]
    public void Create_WithValidData_SetsAllProperties()
    {
        var due = Now.AddDays(3);

        var task = CreateValid(status: TaskStatus.InProgress, dueDate: due);

        task.Id.ShouldNotBe(Guid.Empty);
        task.UserId.ShouldBe(UserId);
        task.Title.ShouldBe("Write report");
        task.Description.ShouldBe("Quarterly numbers");
        task.Status.ShouldBe(TaskStatus.InProgress);
        task.DueDate.ShouldBe(due);
        task.CreatedAt.ShouldBe(Now);
        task.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_TwoTasks_GetDistinctIds()
    {
        CreateValid().Id.ShouldNotBe(CreateValid().Id);
    }

    [Fact]
    public void Create_TitleWithSurroundingWhitespace_TrimsTitle()
    {
        CreateValid(title: "  Write report  ").Title.ShouldBe("Write report");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_MissingTitle_Throws(string? title)
    {
        Should.Throw<DomainException>(() => CreateValid(title: title!));
    }

    [Fact]
    public void Create_TitleAtMaxLength_Succeeds()
    {
        CreateValid(title: new string('a', TaskItem.TitleMaxLength)).Title.Length.ShouldBe(TaskItem.TitleMaxLength);
    }

    [Fact]
    public void Create_TitleOverMaxLength_Throws()
    {
        Should.Throw<DomainException>(() => CreateValid(title: new string('a', TaskItem.TitleMaxLength + 1)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankDescription_StoresNull(string? description)
    {
        CreateValid(description: description).Description.ShouldBeNull();
    }

    [Fact]
    public void Create_DescriptionOverMaxLength_Throws()
    {
        Should.Throw<DomainException>(
            () => CreateValid(description: new string('a', TaskItem.DescriptionMaxLength + 1)));
    }

    [Fact]
    public void Create_UndefinedStatus_Throws()
    {
        Should.Throw<DomainException>(() => CreateValid(status: (TaskStatus)99));
    }

    [Fact]
    public void Create_EmptyUserId_Throws()
    {
        Should.Throw<DomainException>(
            () => TaskItem.Create(Guid.Empty, "Title", null, TaskStatus.Todo, null, Now));
    }

    [Fact]
    public void Create_NonUtcDueDate_Throws()
    {
        var localDue = DateTime.SpecifyKind(Now.AddDays(1), DateTimeKind.Unspecified);

        Should.Throw<DomainException>(() => CreateValid(dueDate: localDue));
    }

    [Fact]
    public void Create_NonUtcNow_Throws()
    {
        var localNow = DateTime.SpecifyKind(Now, DateTimeKind.Local);

        Should.Throw<DomainException>(
            () => TaskItem.Create(UserId, "Title", null, TaskStatus.Todo, null, localNow));
    }

    [Fact]
    public void Update_WithValidData_ReplacesFieldsAndSetsUpdatedAt()
    {
        var task = CreateValid(dueDate: Now.AddDays(1));
        var later = Now.AddHours(2);

        task.Update("  New title ", null, TaskStatus.Done, null, later);

        task.Title.ShouldBe("New title");
        task.Description.ShouldBeNull();
        task.Status.ShouldBe(TaskStatus.Done);
        task.DueDate.ShouldBeNull();
        task.UpdatedAt.ShouldBe(later);
        task.CreatedAt.ShouldBe(Now);
    }

    [Fact]
    public void Update_InvalidTitle_ThrowsAndLeavesTaskUnchanged()
    {
        var task = CreateValid();

        Should.Throw<DomainException>(() => task.Update(" ", "changed", TaskStatus.Done, null, Now.AddHours(1)));

        task.Title.ShouldBe("Write report");
        task.Description.ShouldBe("Quarterly numbers");
        task.Status.ShouldBe(TaskStatus.Todo);
        task.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void ChangeStatus_ValidStatus_SetsStatusAndUpdatedAt()
    {
        var task = CreateValid();
        var later = Now.AddMinutes(5);

        task.ChangeStatus(TaskStatus.Done, later);

        task.Status.ShouldBe(TaskStatus.Done);
        task.UpdatedAt.ShouldBe(later);
    }

    [Fact]
    public void ChangeStatus_UndefinedStatus_Throws()
    {
        var task = CreateValid();

        Should.Throw<DomainException>(() => task.ChangeStatus((TaskStatus)(-1), Now));
        task.Status.ShouldBe(TaskStatus.Todo);
    }
}
