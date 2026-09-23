using System.Net;
using System.Text.Json;
using TaskManager.Api.IntegrationTests.Infrastructure;

namespace TaskManager.Api.IntegrationTests.CrossCutting;

public sealed class ApiDocumentationTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private async Task<JsonElement> GetOpenApiDocumentAsync()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static bool RequiresBearer(JsonElement operation) =>
        operation.TryGetProperty("security", out var security)
        && security.EnumerateArray().Any(requirement => requirement.TryGetProperty("Bearer", out _));

    [Fact]
    public async Task OpenApiDocument_DeclaresJwtBearerScheme()
    {
        var document = await GetOpenApiDocumentAsync();

        var scheme = document.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        scheme.GetProperty("type").GetString().ShouldBe("http");
        scheme.GetProperty("scheme").GetString().ShouldBe("bearer");
        scheme.GetProperty("bearerFormat").GetString().ShouldBe("JWT");
    }

    [Fact]
    public async Task OpenApiDocument_RequiresBearerOnlyOnProtectedOperations()
    {
        var paths = (await GetOpenApiDocumentAsync()).GetProperty("paths");

        RequiresBearer(paths.GetProperty("/api/tasks").GetProperty("get")).ShouldBeTrue();
        RequiresBearer(paths.GetProperty("/api/tasks/{id}").GetProperty("delete")).ShouldBeTrue();
        RequiresBearer(paths.GetProperty("/api/auth/me").GetProperty("get")).ShouldBeTrue();
        RequiresBearer(paths.GetProperty("/api/auth/login").GetProperty("post")).ShouldBeFalse();
        RequiresBearer(paths.GetProperty("/api/auth/register").GetProperty("post")).ShouldBeFalse();
    }

    [Fact]
    public async Task OpenApiDocument_SchemasUseSnakeCaseAndStringEnums()
    {
        var schemas = (await GetOpenApiDocumentAsync()).GetProperty("components").GetProperty("schemas");

        schemas.GetProperty("LoginResponse").GetProperty("properties").TryGetProperty("access_token", out _).ShouldBeTrue();
        schemas.GetProperty("TaskResponse").GetProperty("properties").TryGetProperty("due_date", out _).ShouldBeTrue();
        schemas.GetProperty("TaskStatus").GetProperty("enum").EnumerateArray()
            .Select(value => value.GetString())
            .ShouldBe(["Todo", "InProgress", "Done"]);
    }

    [Fact]
    public async Task OpenApiDocument_DescribesTaskListQueryParameters()
    {
        var parameters = (await GetOpenApiDocumentAsync())
            .GetProperty("paths").GetProperty("/api/tasks").GetProperty("get").GetProperty("parameters")
            .EnumerateArray()
            .ToDictionary(parameter => parameter.GetProperty("name").GetString()!);

        parameters.Keys.ShouldBe(["status", "due_after", "due_before", "sort", "page", "page_size"], ignoreOrder: true);
        parameters["due_before"].GetProperty("description").GetString()!.ShouldContain("exclusive");
        parameters["sort"].GetProperty("description").GetString()!.ShouldContain("-due_date");
    }

    [Fact]
    public async Task OpenApiDocument_AdvertisesOnlyJsonMediaTypes()
    {
        var paths = (await GetOpenApiDocumentAsync()).GetProperty("paths");

        var mediaTypes = paths.EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject())
            .SelectMany(operation => operation.Value.GetProperty("responses").EnumerateObject())
            .Where(response => response.Value.TryGetProperty("content", out _))
            .SelectMany(response => response.Value.GetProperty("content").EnumerateObject().Select(media => media.Name))
            .Distinct()
            .ToList();

        mediaTypes.ShouldNotBeEmpty();
        mediaTypes.ShouldNotContain("text/plain");
        mediaTypes.ShouldNotContain("text/json");
    }

    [Fact]
    public async Task OpenApiDocument_NumericSchemasAreIntegersOnly()
    {
        var schemas = (await GetOpenApiDocumentAsync()).GetProperty("components").GetProperty("schemas");

        schemas.GetProperty("PagedResponseOfTaskResponse").GetProperty("properties").GetProperty("total_count")
            .GetProperty("type").GetString().ShouldBe("integer");
    }

    [Fact]
    public async Task ScalarUi_IsServed()
    {
        var response = await factory.CreateClient().GetAsync("/scalar");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/html");
    }
}
