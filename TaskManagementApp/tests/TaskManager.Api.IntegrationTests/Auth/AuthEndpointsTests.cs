using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaskManager.Api.IntegrationTests.Infrastructure;
using TaskManager.Application.Auth;

namespace TaskManager.Api.IntegrationTests.Auth;

public sealed class AuthEndpointsTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_ValidRequest_Returns201WithIdAndNormalizedEmail()
    {
        var email = ApiClientExtensions.UniqueEmail("Alice").ToUpperInvariant();

        var response = await _client.RegisterAsync(email);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.ReadAsAsync<RegisterResponse>();
        body.Id.ShouldNotBe(Guid.Empty);
        body.Email.ShouldBe(email.ToLowerInvariant());
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.AbsolutePath.ShouldBe("/api/auth/me");
    }

    [Fact]
    public async Task Register_EmailAlreadyUsedInDifferentCase_Returns409Problem()
    {
        var email = ApiClientExtensions.UniqueEmail();
        (await _client.RegisterAsync(email)).EnsureSuccessStatusCode();

        var response = await _client.RegisterAsync(email.ToUpperInvariant());

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Register_InvalidPayload_Returns400WithSnakeCaseFieldErrors()
    {
        var response = await _client.RegisterAsync("not-an-email", "short");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.ReadAsAsync<ValidationProblemDetails>();
        problem.Errors.Keys.ShouldBe(["email", "password"], ignoreOrder: true);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithBearerTokenInSnakeCase()
    {
        var email = ApiClientExtensions.UniqueEmail();
        (await _client.RegisterAsync(email)).EnsureSuccessStatusCode();

        var response = await _client.LoginAsync(email);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        root.GetProperty("access_token").GetString().ShouldNotBeNullOrWhiteSpace();
        root.GetProperty("token_type").GetString().ShouldBe("Bearer");
        root.GetProperty("expires_at").GetDateTime().ShouldBeInRange(DateTime.UtcNow.AddMinutes(55), DateTime.UtcNow.AddMinutes(61));
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_Return401WithIdenticalMessage()
    {
        var email = ApiClientExtensions.UniqueEmail();
        (await _client.RegisterAsync(email)).EnsureSuccessStatusCode();

        var wrongPassword = await _client.LoginAsync(email, "Wr0ngPassword");
        var unknownEmail = await _client.LoginAsync(ApiClientExtensions.UniqueEmail());

        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var wrongPasswordProblem = await wrongPassword.ReadAsAsync<ProblemDetails>();
        var unknownEmailProblem = await unknownEmail.ReadAsAsync<ProblemDetails>();
        wrongPasswordProblem.Detail.ShouldBe(unknownEmailProblem.Detail);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithTamperedToken_Returns401()
    {
        await _client.AuthenticateAsNewUserAsync();
        var token = _client.DefaultRequestHeaders.Authorization!.Parameter!;
        var tampered = token[..^4] + (token.EndsWith("AAAA", StringComparison.Ordinal) ? "BBBB" : "AAAA");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tampered);

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithValidToken_Returns200WithCurrentUser()
    {
        var email = await _client.AuthenticateAsNewUserAsync();

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.ReadAsAsync<CurrentUserResponse>();
        body.Email.ShouldBe(email);
        body.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    public void Startup_MissingOrShortJwtKey_FailsFast(string key)
    {
        using var misconfigured = factory.WithWebHostBuilder(builder => builder.UseSetting("Jwt:Key", key));

        var exception = Should.Throw<OptionsValidationException>(() => misconfigured.CreateClient());

        exception.Message.ShouldContain("Jwt:Key");
    }
}
