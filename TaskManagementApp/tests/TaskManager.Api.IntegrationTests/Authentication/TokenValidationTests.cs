using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Api.IntegrationTests.Infrastructure;
using TaskManager.Domain.Users;
using TaskManager.Infrastructure.Authentication;

namespace TaskManager.Api.IntegrationTests.Authentication;

/// <summary>
/// Crafts tokens with the production generator but altered settings or clock, and checks the API's
/// bearer validation (issuer, audience, key, lifetime and the 1 minute clock skew).
/// </summary>
public sealed class TokenValidationTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private const int ExpiryMinutes = 60;

    private JwtOptions ApiOptions()
    {
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        return new JwtOptions
        {
            Issuer = configuration["Jwt:Issuer"]!,
            Audience = configuration["Jwt:Audience"]!,
            Key = TaskManagerApiFactory.TestJwtKey,
            ExpiryMinutes = ExpiryMinutes,
        };
    }

    /// <summary>A token whose expiry is <paramref name="expiresFromNow"/> away (negative = already expired).</summary>
    private static string CreateToken(JwtOptions options, TimeSpan expiresFromNow)
    {
        var issuedAt = DateTime.UtcNow.Add(expiresFromNow).AddMinutes(-options.ExpiryMinutes);
        var generator = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(options), new FixedDateTimeProvider(issuedAt));
        return generator.Generate(User.Create("someone@example.com", "hash", issuedAt)).Token;
    }

    private async Task<HttpStatusCode> ListTasksWithAsync(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (await client.GetAsync("/api/tasks")).StatusCode;
    }

    [Fact]
    public async Task ValidToken_IsAccepted()
    {
        (await ListTasksWithAsync(CreateToken(ApiOptions(), TimeSpan.FromMinutes(30)))).ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TokenExpiredWithinClockSkew_IsStillAccepted()
    {
        (await ListTasksWithAsync(CreateToken(ApiOptions(), TimeSpan.FromSeconds(-20)))).ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TokenExpiredBeyondClockSkew_Returns401()
    {
        (await ListTasksWithAsync(CreateToken(ApiOptions(), TimeSpan.FromMinutes(-2)))).ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenFromAnotherIssuer_Returns401()
    {
        var options = ApiOptions();
        var token = CreateToken(new JwtOptions { Issuer = "evil-issuer", Audience = options.Audience, Key = options.Key, ExpiryMinutes = ExpiryMinutes }, TimeSpan.FromMinutes(30));

        (await ListTasksWithAsync(token)).ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenForAnotherAudience_Returns401()
    {
        var options = ApiOptions();
        var token = CreateToken(new JwtOptions { Issuer = options.Issuer, Audience = "other-app", Key = options.Key, ExpiryMinutes = ExpiryMinutes }, TimeSpan.FromMinutes(30));

        (await ListTasksWithAsync(token)).ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenSignedWithAnotherKey_Returns401()
    {
        var options = ApiOptions();
        var token = CreateToken(new JwtOptions { Issuer = options.Issuer, Audience = options.Audience, Key = new string('k', 64), ExpiryMinutes = ExpiryMinutes }, TimeSpan.FromMinutes(30));

        (await ListTasksWithAsync(token)).ShouldBe(HttpStatusCode.Unauthorized);
    }
}
