using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using JournalApp.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;

namespace JournalApp.Api.Tests;

[Collection(ApiCollection.Name)]
public class AuthEndpointsTests(JournalApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_Anonymous_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_ValidRequest_Returns201WithUserAndNoPassword()
    {
        // Arrange
        var (username, email) = ApiClientExtensions.NewCredentials();

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { username, email = email.ToUpperInvariant(), password = "Password1" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContainEquivalentOf("password");
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(ApiClientExtensions.Json);
        user!.Username.Should().Be(username);
        user.Email.Should().Be(email);
    }

    [Fact]
    public async Task Register_InvalidRequest_Returns400WithFieldErrors()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { username = "ab", email = "not-an-email", password = "weak" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Keys.Should().BeEquivalentTo("username", "email", "password");
    }

    [Fact]
    public async Task Register_DuplicateEmailWithDifferentCase_Returns409()
    {
        // Arrange
        var (username, email) = ApiClientExtensions.NewCredentials();
        (await _client.PostAsJsonAsync("/api/auth/register", new { username, email, password = "Password1" }))
            .EnsureSuccessStatusCode();

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { username = username + "x", email = email.ToUpperInvariant(), password = "Password1" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Register_DuplicateUsername_Returns409()
    {
        // Arrange
        var (username, email) = ApiClientExtensions.NewCredentials();
        (await _client.PostAsJsonAsync("/api/auth/register", new { username, email, password = "Password1" }))
            .EnsureSuccessStatusCode();

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { username, email = "other" + email, password = "Password1" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        // Arrange
        var (username, email) = ApiClientExtensions.NewCredentials();
        await _client.PostAsJsonAsync("/api/auth/register", new { username, email, password = "Password1" });

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password1" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResult>(ApiClientExtensions.Json);
        auth!.Token.Should().NotBeNullOrWhiteSpace();
        auth.User.Username.Should().Be(username);
        auth.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange
        var (username, email) = ApiClientExtensions.NewCredentials();
        await _client.PostAsJsonAsync("/api/auth/register", new { username, email, password = "Password1" });

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong1234" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@journal.com", password = "Password1" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithInvalidToken_Returns401()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithToken_Returns200WithCurrentUser()
    {
        // Arrange
        var (client, auth) = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(ApiClientExtensions.Json);
        user!.Id.Should().Be(auth.User.Id);
    }

    [Fact]
    public async Task UnknownApiRoute_Returns404InsteadOfSpaFallback()
    {
        // Act
        var response = await _client.GetAsync("/api/does-not-exist");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().NotBe("text/html");
    }
}
