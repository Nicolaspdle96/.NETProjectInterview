using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.IntegrationTests.Infrastructure;
using TaskManager.Application.Common;
using TaskManager.Application.Tasks;

namespace TaskManager.Api.IntegrationTests.Tasks;

public sealed class TasksEndpointsTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private static readonly DateTime Tomorrow = DateTime.UtcNow.Date.AddDays(1);

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient();
        await client.AuthenticateAsNewUserAsync();
        return client;
    }

    private static async Task<TaskResponse> CreateTaskAsync(HttpClient client, string title = "Write report")
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new { title, description = "Q3 numbers", due_date = Tomorrow });
        response.EnsureSuccessStatusCode();
        return await response.ReadAsAsync<TaskResponse>();
    }

    [Theory]
    [InlineData("GET", "/api/tasks")]
    [InlineData("GET", "/api/tasks/7f1b3a52-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/tasks")]
    [InlineData("PUT", "/api/tasks/7f1b3a52-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/tasks/7f1b3a52-0000-0000-0000-000000000001")]
    public async Task AnyEndpoint_WithoutToken_Returns401(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { title = "x", status = "Todo" });
        }

        var response = await factory.CreateClient().SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocationAndSnakeCaseBody()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/tasks", new { title = "  Write report ", due_date = "2099-10-01T00:00:00Z" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        var id = root.GetProperty("id").GetGuid();
        root.GetProperty("title").GetString().ShouldBe("Write report");
        root.GetProperty("description").ValueKind.ShouldBe(JsonValueKind.Null);
        root.GetProperty("status").GetString().ShouldBe("Todo");
        root.GetProperty("due_date").GetString().ShouldBe("2099-10-01T00:00:00Z");
        root.GetProperty("created_at").GetDateTime().ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddSeconds(1));
        root.GetProperty("updated_at").ValueKind.ShouldBe(JsonValueKind.Null);
        response.Headers.Location!.AbsolutePath.ShouldBe($"/api/tasks/{id}");
    }

    [Fact]
    public async Task Create_InvalidRequest_Returns400WithSnakeCaseFieldErrors()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/tasks", new { title = " ", due_date = DateTime.UtcNow.Date.AddDays(-1) });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.ReadAsAsync<ValidationProblemDetails>();
        problem.Errors.Keys.ShouldBe(["title", "due_date"], ignoreOrder: true);
    }

    [Fact]
    public async Task Create_UnknownStatusName_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/tasks", new { title = "Task", status = "Archived" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FullLifecycle_CreateGetUpdateDelete_Works()
    {
        var client = await CreateAuthenticatedClientAsync();
        var created = await CreateTaskAsync(client);

        var fetched = await client.GetAsync($"/api/tasks/{created.Id}");
        fetched.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await fetched.ReadAsAsync<TaskResponse>()).ShouldBe(created);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/tasks/{created.Id}",
            new { title = "Report sent", description = (string?)null, status = "Done", due_date = (DateTime?)null });
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.ReadAsAsync<TaskResponse>();
        updated.Title.ShouldBe("Report sent");
        updated.Description.ShouldBeNull();
        updated.Status.ShouldBe(TaskStatus.Done);
        updated.DueDate.ShouldBeNull();
        updated.CreatedAt.ShouldBe(created.CreatedAt);
        updated.UpdatedAt.ShouldNotBeNull();

        (await client.DeleteAsync($"/api/tasks/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/tasks/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.DeleteAsync($"/api/tasks/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_MissingStatus_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();
        var created = await CreateTaskAsync(client);

        var response = await client.PutAsJsonAsync($"/api/tasks/{created.Id}", new { title = "No status" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadAsAsync<ValidationProblemDetails>()).Errors.Keys.ShouldContain("status");
    }

    [Fact]
    public async Task List_ReturnsOnlyOwnTasksNewestFirstWithPagingEnvelope()
    {
        var alice = await CreateAuthenticatedClientAsync();
        var bob = await CreateAuthenticatedClientAsync();
        await CreateTaskAsync(alice, "First");
        await CreateTaskAsync(alice, "Second");
        await CreateTaskAsync(alice, "Third");
        await CreateTaskAsync(bob, "Bob's task");

        var response = await alice.GetAsync("/api/tasks?page=1&page_size=2");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("page").GetInt32().ShouldBe(1);
        json.RootElement.GetProperty("page_size").GetInt32().ShouldBe(2);
        json.RootElement.GetProperty("total_count").GetInt32().ShouldBe(3);
        var page = await response.ReadAsAsync<PagedResponse<TaskResponse>>();
        page.Items.Select(task => task.Title).ShouldBe(["Third", "Second"]);
    }

    [Fact]
    public async Task List_DefaultsToFirstPageOfTwenty()
    {
        var client = await CreateAuthenticatedClientAsync();

        var page = await (await client.GetAsync("/api/tasks")).ReadAsAsync<PagedResponse<TaskResponse>>();

        page.Page.ShouldBe(1);
        page.PageSize.ShouldBe(20);
        page.TotalCount.ShouldBe(0);
        page.Items.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page_size=101")]
    public async Task List_OutOfRangePaging_Returns400(string query)
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/tasks?{query}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_WithStatusDueRangeAndSort_AppliesAllQueryParameters()
    {
        var client = await CreateAuthenticatedClientAsync();
        async Task Create(string title, string status, string? dueDate) =>
            (await client.PostAsJsonAsync("/api/tasks", new { title, status, due_date = dueDate })).EnsureSuccessStatusCode();

        await Create("Todo early", "Todo", "2099-01-10T00:00:00Z");
        await Create("Todo late", "Todo", "2099-03-01T00:00:00Z");
        await Create("Todo mid", "Todo", "2099-02-01T00:00:00Z");
        await Create("Todo undated", "Todo", null);
        await Create("Done mid", "Done", "2099-02-01T00:00:00Z");

        var ranged = await (await client.GetAsync(
                "/api/tasks?status=Todo&due_after=2099-01-10T00:00:00Z&due_before=2099-03-01T00:00:00Z&sort=-due_date"))
            .ReadAsAsync<PagedResponse<TaskResponse>>();
        var byDueDate = await (await client.GetAsync("/api/tasks?status=todo&sort=due_date"))
            .ReadAsAsync<PagedResponse<TaskResponse>>();

        ranged.Items.Select(task => task.Title).ShouldBe(["Todo mid", "Todo early"]);
        ranged.TotalCount.ShouldBe(2);
        byDueDate.Items.Select(task => task.Title).ShouldBe(["Todo early", "Todo mid", "Todo late", "Todo undated"]);
    }

    [Theory]
    [InlineData("sort=title", "sort")]
    [InlineData("status=Archived", "status")]
    [InlineData("due_after=2099-02-01&due_before=2099-01-01", "due_after")]
    [InlineData("due_before=not-a-date", "due_before")]
    public async Task List_InvalidQueryParameter_Returns400NamingTheParameter(string query, string expectedKey)
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/tasks?{query}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadAsAsync<ValidationProblemDetails>()).Errors.Keys.ShouldContain(expectedKey);
    }

    [Fact]
    public async Task OtherUser_CannotReadUpdateOrDeleteTask_Gets404AndTaskIsUntouched()
    {
        var owner = await CreateAuthenticatedClientAsync();
        var intruder = await CreateAuthenticatedClientAsync();
        var task = await CreateTaskAsync(owner, "Private");
        var url = $"/api/tasks/{task.Id}";

        var get = await intruder.GetAsync(url);
        var put = await intruder.PutAsJsonAsync(url, new { title = "Hijacked", status = "Done" });
        var delete = await intruder.DeleteAsync(url);

        get.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        put.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        delete.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var intruderList = await (await intruder.GetAsync("/api/tasks")).ReadAsAsync<PagedResponse<TaskResponse>>();
        intruderList.TotalCount.ShouldBe(0);

        var ownerView = await (await owner.GetAsync(url)).ReadAsAsync<TaskResponse>();
        ownerView.ShouldBe(task);
    }

    [Fact]
    public async Task OtherUsersTask_ReturnsSameResponseAsNonexistentTask()
    {
        var owner = await CreateAuthenticatedClientAsync();
        var intruder = await CreateAuthenticatedClientAsync();
        var task = await CreateTaskAsync(owner);

        var foreign = await (await intruder.GetAsync($"/api/tasks/{task.Id}")).ReadAsAsync<ProblemDetails>();
        var missing = await (await intruder.GetAsync($"/api/tasks/{Guid.NewGuid()}")).ReadAsAsync<ProblemDetails>();

        foreign.Status.ShouldBe(missing.Status);
        foreign.Title.ShouldBe(missing.Title);
        foreign.Detail.ShouldBe(missing.Detail);
    }
}
