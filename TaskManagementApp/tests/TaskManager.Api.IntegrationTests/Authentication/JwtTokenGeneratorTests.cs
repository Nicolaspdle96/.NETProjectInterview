using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Api.IntegrationTests.Infrastructure;
using TaskManager.Domain.Users;
using TaskManager.Infrastructure.Authentication;

namespace TaskManager.Api.IntegrationTests.Authentication;

public sealed class JwtTokenGeneratorTests
{
    private static readonly DateTime Now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

    private static readonly JwtOptions Options = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        Key = TaskManagerApiFactory.TestJwtKey,
        ExpiryMinutes = 30,
    };

    private readonly User _user = User.Create("alice@example.com", "hash", Now);
    private readonly JwtTokenGenerator _sut = new(Microsoft.Extensions.Options.Options.Create(Options), new FixedDateTimeProvider(Now));

    [Fact]
    public void Generate_ReturnsExpiryBasedOnConfiguredMinutes()
    {
        _sut.Generate(_user).ExpiresAt.ShouldBe(Now.AddMinutes(30));
    }

    [Fact]
    public void Generate_TokenCarriesRequiredClaimsAndMetadata()
    {
        var token = new JsonWebTokenHandler().ReadJsonWebToken(_sut.Generate(_user).Token);

        token.Subject.ShouldBe(_user.Id.ToString());
        token.GetClaim(JwtRegisteredClaimNames.Email).Value.ShouldBe("alice@example.com");
        token.Id.ShouldNotBeNullOrWhiteSpace();
        token.Issuer.ShouldBe("test-issuer");
        token.Audiences.ShouldBe(["test-audience"]);
        token.Alg.ShouldBe(SecurityAlgorithms.HmacSha256);
        token.IssuedAt.ShouldBe(Now);
        token.ValidTo.ShouldBe(Now.AddMinutes(30));
    }

    [Fact]
    public void Generate_TokenDoesNotContainPasswordHash()
    {
        var token = new JsonWebTokenHandler().ReadJsonWebToken(_sut.Generate(_user).Token);

        token.Claims.ShouldNotContain(claim => claim.Value == _user.PasswordHash);
    }

    [Fact]
    public void Generate_EachTokenHasUniqueJti()
    {
        var handler = new JsonWebTokenHandler();

        var first = handler.ReadJsonWebToken(_sut.Generate(_user).Token).Id;
        var second = handler.ReadJsonWebToken(_sut.Generate(_user).Token).Id;

        first.ShouldNotBe(second);
    }

    [Fact]
    public async Task Generate_TokenSignatureValidatesWithConfiguredKey()
    {
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(
            _sut.Generate(_user).Token,
            new TokenValidationParameters
            {
                ValidIssuer = "test-issuer",
                ValidAudience = "test-audience",
                IssuerSigningKey = Options.GetSigningKey(),
                ValidateLifetime = false,
            });

        result.IsValid.ShouldBeTrue();
    }
}
