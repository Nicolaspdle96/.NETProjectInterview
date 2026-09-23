using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManager.Application.Auth;

namespace TaskManager.Api.IntegrationTests.Infrastructure;

internal static class ApiClientExtensions
{
    public const string DefaultPassword = "Passw0rd!";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string UniqueEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@example.com";

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email, string password = DefaultPassword) =>
        client.PostAsJsonAsync("/api/auth/register", new { email, password });

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password = DefaultPassword) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    /// <summary>Registers a fresh user, logs in and attaches the bearer token to <paramref name="client"/>.</summary>
    public static async Task<string> AuthenticateAsNewUserAsync(this HttpClient client, string? email = null)
    {
        email ??= UniqueEmail();
        (await client.RegisterAsync(email)).EnsureSuccessStatusCode();

        var login = await client.LoginAsync(email);
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>(Json);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return email;
    }

    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;
}
