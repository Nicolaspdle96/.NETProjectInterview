using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Api.IntegrationTests.Infrastructure;
using TaskManager.Application.Auth;
using TaskManager.Application.Common.Results;

namespace TaskManager.Api.IntegrationTests.CrossCutting;

public sealed class ErrorHandlingTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private const string InternalDetail = "connection string leaked: Server=secret";

    private HttpClient CreateClientWithFailingLogin(string environment) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureTestServices(services => services.AddScoped<IAuthService, ThrowingAuthService>());
        }).CreateClient();

    [Fact]
    public async Task UnhandledException_OutsideDevelopment_Returns500ProblemWithoutInternals()
    {
        var client = CreateClientWithFailingLogin("Production");

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "a@b.c", password = "x" });

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain("secret");
        body.ShouldNotContain("ThrowingAuthService");
        body.ShouldNotContain("stack_trace");
        var problem = await response.ReadAsAsync<ProblemDetails>();
        problem.Status.ShouldBe(500);
        problem.Detail.ShouldBeNull();
    }

    [Fact]
    public async Task UnhandledException_InDevelopment_IncludesExceptionDetails()
    {
        var client = CreateClientWithFailingLogin("Development");

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "a@b.c", password = "x" });

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var problem = await response.ReadAsAsync<ProblemDetails>();
        problem.Detail.ShouldBe(InternalDetail);
        problem.Extensions.ShouldContainKey("exception");
    }

    [Fact]
    public async Task MissingToken_Returns401ProblemWithBearerChallenge()
    {
        var response = await factory.CreateClient().GetAsync("/api/tasks");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        response.Headers.WwwAuthenticate.ShouldContain(header => header.Scheme == "Bearer");
        var problem = await response.ReadAsAsync<ProblemDetails>();
        problem.Status.ShouldBe(401);
        problem.Instance.ShouldBe("/api/tasks");
    }

    [Fact]
    public async Task UnknownRoute_Returns404Problem()
    {
        var response = await factory.CreateClient().GetAsync("/api/does-not-exist");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task MalformedJsonBody_Returns400Problem()
    {
        var content = new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json");

        var response = await factory.CreateClient().PostAsync("/api/auth/login", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    private sealed class ThrowingAuthService : IAuthService
    {
        public Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(InternalDetail);

        public Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(InternalDetail);

        public Task<Result<CurrentUserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException(InternalDetail);
    }
}
