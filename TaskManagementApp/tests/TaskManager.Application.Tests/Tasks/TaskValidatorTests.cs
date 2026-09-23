using TaskManager.Application.Tasks;
using TaskManager.Application.Tests.Fakes;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tests.Tasks;

public sealed class TaskValidatorTests
{
    private static readonly DateTime Now = new(2026, 9, 23, 15, 30, 0, DateTimeKind.Utc);

    private readonly CreateTaskRequestValidator _createValidator = new(new FixedDateTimeProvider(Now));
    private readonly UpdateTaskRequestValidator _updateValidator = new();
    private readonly ListTasksRequestValidator _listValidator = new();

    private static CreateTaskRequest Create(
        string title = "Title",
        string? description = null,
        TaskStatus? status = null,
        DateTime? dueDate = null) => new(title, description, status, dueDate);

    [Fact]
    public void Create_MinimalValidRequest_Passes()
    {
        _createValidator.Validate(Create()).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankTitle_Fails(string title)
    {
        _createValidator.Validate(Create(title)).Errors.ShouldContain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Create_TitleAtMaxLengthAfterTrim_Passes()
    {
        var title = "  " + new string('a', TaskItem.TitleMaxLength) + "  ";

        _createValidator.Validate(Create(title)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Create_TitleOverMaxLength_Fails()
    {
        _createValidator.Validate(Create(new string('a', TaskItem.TitleMaxLength + 1)))
            .Errors.ShouldContain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Create_DescriptionOverMaxLength_Fails()
    {
        _createValidator.Validate(Create(description: new string('a', TaskItem.DescriptionMaxLength + 1)))
            .Errors.ShouldContain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Create_UndefinedStatus_Fails()
    {
        _createValidator.Validate(Create(status: (TaskStatus)42)).Errors.ShouldContain(e => e.PropertyName == "Status");
    }

    [Fact]
    public void Create_DueDateEarlierToday_Passes()
    {
        var earlierToday = new DateTime(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc);

        _createValidator.Validate(Create(dueDate: earlierToday)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Create_DueDateYesterday_Fails()
    {
        var yesterday = new DateTime(2026, 9, 22, 23, 59, 59, DateTimeKind.Utc);

        _createValidator.Validate(Create(dueDate: yesterday)).Errors.ShouldContain(e => e.PropertyName == "DueDate");
    }

    [Fact]
    public void Create_DateOnlyDueDateToday_Passes()
    {
        var dateOnly = new DateTime(2026, 9, 23, 0, 0, 0, DateTimeKind.Unspecified);

        _createValidator.Validate(Create(dueDate: dateOnly)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Update_ValidRequest_Passes()
    {
        _updateValidator.Validate(new UpdateTaskRequest("Title", null, TaskStatus.Done, Now.AddYears(-1)))
            .IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Update_MissingStatus_Fails()
    {
        _updateValidator.Validate(new UpdateTaskRequest("Title", null, null, null))
            .Errors.ShouldContain(e => e.PropertyName == "Status");
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, ListTasksRequest.MaxPageSize + 1)]
    public void List_OutOfRangePaging_Fails(int page, int pageSize)
    {
        _listValidator.Validate(new ListTasksRequest(page, pageSize)).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(7, ListTasksRequest.MaxPageSize)]
    public void List_InRangePaging_Passes(int page, int pageSize)
    {
        _listValidator.Validate(new ListTasksRequest(page, pageSize)).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("created_at")]
    [InlineData("-created_at")]
    [InlineData("due_date")]
    [InlineData("-DUE_DATE")]
    public void List_SupportedSort_Passes(string? sort)
    {
        _listValidator.Validate(new ListTasksRequest(Sort: sort)).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("title")]
    [InlineData("--due_date")]
    [InlineData("-")]
    [InlineData("due_date,created_at")]
    public void List_UnsupportedSort_Fails(string sort)
    {
        _listValidator.Validate(new ListTasksRequest(Sort: sort)).Errors.ShouldContain(e => e.PropertyName == "Sort");
    }

    [Fact]
    public void List_UndefinedStatus_Fails()
    {
        _listValidator.Validate(new ListTasksRequest(Status: (TaskStatus)9)).Errors.ShouldContain(e => e.PropertyName == "Status");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void List_DueAfterNotEarlierThanDueBefore_Fails(int daysAfterStart)
    {
        var request = new ListTasksRequest(DueAfter: Now.AddDays(daysAfterStart), DueBefore: Now);

        _listValidator.Validate(request).Errors.ShouldContain(e => e.PropertyName == "DueAfter");
    }

    [Fact]
    public void List_DueRangeInOrderOrOpenEnded_Passes()
    {
        _listValidator.Validate(new ListTasksRequest(DueAfter: Now, DueBefore: Now.AddDays(1))).IsValid.ShouldBeTrue();
        _listValidator.Validate(new ListTasksRequest(DueAfter: Now)).IsValid.ShouldBeTrue();
        _listValidator.Validate(new ListTasksRequest(DueBefore: Now)).IsValid.ShouldBeTrue();
    }
}
