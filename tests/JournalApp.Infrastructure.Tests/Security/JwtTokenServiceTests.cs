using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using JournalApp.Domain.Entities;
using JournalApp.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JournalApp.Infrastructure.Tests.Security;

public class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions Options = new()
    {
        Issuer = "JournalApp.Tests",
        Audience = "JournalApp.Tests.Client",
        Key = "test-signing-key-that-is-long-enough-for-hmac-sha256",
        ExpiresMinutes = 30
    };

    private readonly JwtTokenService _sut = new(Microsoft.Extensions.Options.Options.Create(Options), new FixedTime(Now));
    private readonly User _user = User.Create("demo", "demo@journal.com", "hash", Now.UtcDateTime);

    [Fact]
    public void GenerateToken_User_ContainsSubEmailAndUniqueNameClaims()
    {
        // Act
        var result = _sut.GenerateToken(_user);

        // Assert
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == _user.Id.ToString());
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "demo@journal.com");
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == "demo");
        token.Issuer.Should().Be(Options.Issuer);
        token.Audiences.Should().ContainSingle().Which.Should().Be(Options.Audience);
    }

    [Fact]
    public void GenerateToken_User_ExpiresAfterConfiguredMinutes()
    {
        // Act
        var result = _sut.GenerateToken(_user);

        // Assert
        var expected = Now.UtcDateTime.AddMinutes(Options.ExpiresMinutes);
        result.ExpiresAt.Should().Be(expected);
        new JwtSecurityTokenHandler().ReadJwtToken(result.Token).ValidTo.Should().Be(expected);
    }

    [Fact]
    public async Task GenerateToken_User_IsSignedWithConfiguredKey()
    {
        // Arrange
        var result = _sut.GenerateToken(_user);
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.Key)),
            ValidateLifetime = false
        };

        // Act
        var validation = await new JwtSecurityTokenHandler().ValidateTokenAsync(result.Token, parameters);

        // Assert
        validation.IsValid.Should().BeTrue();
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
