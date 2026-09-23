using TaskManager.Application.Auth;
using TaskManager.Application.Common.Results;
using TaskManager.Application.Tasks;
using TaskManager.Application.Tests.Fakes;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tests.Tasks;

public sealed class TaskServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private readonly InMemoryTaskRepository _tasks = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUserService _currentUser = new() { UserId = OwnerId };
    private readonly FixedDateTimeProvider _clock = new(Now);
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(
            _tasks,
            _unitOfWork,
            _currentUser,
            _clock,
            new CreateTaskRequestValidator(_clock),
            new UpdateTaskRequestValidator(),
            new ListTasksRequestValidator());
    }

    private TaskItem Seed(Guid userId, string title = "Existing", DateTime? createdAt = null)
    {
        var task = TaskItem.Create(userId, title, "details", TaskStatus.Todo, null, createdAt ?? Now.AddDays(-1));
        _tasks.Add(task);
        return task;
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesTaskForCurrentUser()
    {
        var due = Now.AddDays(3);

        var result = await _sut.CreateAsync(new CreateTaskRequest(" Plan ", "Sprint", TaskStatus.InProgress, due), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var stored = _tasks.Tasks.ShouldHaveSingleItem();
        stored.UserId.ShouldBe(OwnerId);
        result.Value.ShouldBe(new TaskResponse(stored.Id, "Plan", "Sprint", TaskStatus.InProgress, due, Now, null));
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task CreateAsync_NoStatus_DefaultsToTodo()
    {
        var result = await _sut.CreateAsync(new CreateTaskRequest("Plan", null, null, null), CancellationToken.None);

        result.Value.Status.ShouldBe(TaskStatus.Todo);
    }

    [Fact]
    public async Task CreateAsync_DueDateWithOffset_IsStoredInUtc()
    {
        var dueWithOffset = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.FromHours(2));

        var result = await _sut.CreateAsync(
            new CreateTaskRequest("Plan", null, null, dueWithOffset.LocalDateTime),
            CancellationToken.None);

        result.Value.DueDate.ShouldBe(new DateTime(2026, 10, 1, 7, 0, 0, DateTimeKind.Utc));
        result.Value.DueDate!.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ReturnsValidationErrorAndSavesNothing()
    {
        var result = await _sut.CreateAsync(new CreateTaskRequest(" ", null, null, Now.AddDays(-1)), CancellationToken.None);

        result.Error!.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors!.Keys.ShouldBe(["Title", "DueDate"], ignoreOrder: true);
        _tasks.Tasks.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetAsync_OwnTask_ReturnsTask()
    {
        var task = Seed(OwnerId);

        var result = await _sut.GetAsync(task.Id, CancellationToken.None);

        result.Value.Id.ShouldBe(task.Id);
    }

    [Fact]
    public async Task GetAsync_TaskOfAnotherUser_ReturnsNotFound()
    {
        var task = Seed(OtherUserId);

        var result = await _sut.GetAsync(task.Id, CancellationToken.None);

        result.Error.ShouldBe(TaskErrors.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_OwnTask_UpdatesFieldsAndTimestamp()
    {
        var task = Seed(OwnerId);
        _clock.UtcNow = Now.AddHours(1);

        var result = await _sut.UpdateAsync(
            task.Id,
            new UpdateTaskRequest("Renamed", null, TaskStatus.Done, null),
            CancellationToken.None);

        result.Value.Title.ShouldBe("Renamed");
        result.Value.Description.ShouldBeNull();
        result.Value.Status.ShouldBe(TaskStatus.Done);
        result.Value.UpdatedAt.ShouldBe(Now.AddHours(1));
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task UpdateAsync_PastDueDate_IsAllowed()
    {
        var task = Seed(OwnerId);

        var result = await _sut.UpdateAsync(
            task.Id,
            new UpdateTaskRequest("Overdue", null, TaskStatus.InProgress, Now.AddDays(-5)),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateAsync_TaskOfAnotherUser_ReturnsNotFoundAndLeavesTaskUnchanged()
    {
        var task = Seed(OtherUserId, "Theirs");

        var result = await _sut.UpdateAsync(
            task.Id,
            new UpdateTaskRequest("Hijacked", null, TaskStatus.Done, null),
            CancellationToken.None);

        result.Error.ShouldBe(TaskErrors.NotFound);
        task.Title.ShouldBe("Theirs");
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateAsync_MissingStatus_ReturnsValidationError()
    {
        var task = Seed(OwnerId);

        var result = await _sut.UpdateAsync(task.Id, new UpdateTaskRequest("Title", null, null, null), CancellationToken.None);

        result.Error!.ValidationErrors!.Keys.ShouldContain("Status");
    }

    [Fact]
    public async Task DeleteAsync_OwnTask_RemovesIt()
    {
        var task = Seed(OwnerId);

        var result = await _sut.DeleteAsync(task.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _tasks.Tasks.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteAsync_TaskOfAnotherUser_ReturnsNotFoundAndKeepsTask()
    {
        var task = Seed(OtherUserId);

        var result = await _sut.DeleteAsync(task.Id, CancellationToken.None);

        result.Error.ShouldBe(TaskErrors.NotFound);
        _tasks.Tasks.ShouldContain(task);
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyOwnTasksWithPagingInfo()
    {
        Seed(OwnerId, "Old", Now.AddDays(-2));
        Seed(OwnerId, "New", Now.AddDays(-1));
        Seed(OtherUserId, "Theirs");

        var result = await _sut.ListAsync(new ListTasksRequest(Page: 1, PageSize: 1), CancellationToken.None);

        result.Value.Items.Select(task => task.Title).ShouldBe(["New"]);
        result.Value.Page.ShouldBe(1);
        result.Value.PageSize.ShouldBe(1);
        result.Value.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task ListAsync_PageSizeOverMaximum_ReturnsValidationError()
    {
        var result = await _sut.ListAsync(new ListTasksRequest(PageSize: ListTasksRequest.MaxPageSize + 1), CancellationToken.None);

        result.Error!.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task AllOperations_Anonymous_ReturnNotAuthenticated()
    {
        _currentUser.UserId = null;
        var id = Guid.NewGuid();

        (await _sut.ListAsync(new ListTasksRequest(), CancellationToken.None)).Error.ShouldBe(AuthErrors.NotAuthenticated);
        (await _sut.GetAsync(id, CancellationToken.None)).Error.ShouldBe(AuthErrors.NotAuthenticated);
        (await _sut.CreateAsync(new CreateTaskRequest("T", null, null, null), CancellationToken.None)).Error.ShouldBe(AuthErrors.NotAuthenticated);
        (await _sut.UpdateAsync(id, new UpdateTaskRequest("T", null, TaskStatus.Todo, null), CancellationToken.None)).Error.ShouldBe(AuthErrors.NotAuthenticated);
        (await _sut.DeleteAsync(id, CancellationToken.None)).Error.ShouldBe(AuthErrors.NotAuthenticated);
    }
}
