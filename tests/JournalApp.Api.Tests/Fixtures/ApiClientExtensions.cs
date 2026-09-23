using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace JournalApp.Api.Tests.Fixtures;

public record UserResponse(Guid Id, string Username, string Email, DateTime CreatedAt);

public record AuthResult(string Token, DateTime ExpiresAt, UserResponse User);

public record EntryResponse(Guid Id, string Title, string Content, string? Mood, DateTime CreatedAt, DateTime? UpdatedAt);

public static class ApiClientExtensions
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public const string DefaultPassword = "Password1";

    public static (string Username, string Email) NewCredentials()
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        return ($"user{suffix}", $"user{suffix}@journal.com");
    }

    /// <summary>Registers a brand-new user, logs in and returns a client authenticated as that user.</summary>
    public static async Task<(HttpClient Client, AuthResult Auth)> CreateAuthenticatedClientAsync(this JournalApiFactory factory)
    {
        var client = factory.CreateClient();
        var (username, email) = NewCredentials();

        var register = await client.PostAsJsonAsync("/api/auth/register", new { username, email, password = DefaultPassword });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = DefaultPassword });
        login.EnsureSuccessStatusCode();
        var auth = (await login.Content.ReadFromJsonAsync<AuthResult>(Json))!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth);
    }

    public static async Task<EntryResponse> CreateEntryAsync(this HttpClient client, string title, string content = "Content", string? mood = "Good")
    {
        var response = await client.PostAsJsonAsync("/api/entries", new { title, content, mood });
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<EntryResponse>(Json))!;
    }
}
