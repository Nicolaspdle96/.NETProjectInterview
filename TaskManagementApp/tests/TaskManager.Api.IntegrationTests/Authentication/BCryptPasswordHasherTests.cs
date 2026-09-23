using TaskManager.Infrastructure.Authentication;

namespace TaskManager.Api.IntegrationTests.Authentication;

public sealed class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_ProducesBcryptHashWithWorkFactor12()
    {
        var hash = _sut.Hash("Passw0rd!");

        hash.ShouldNotContain("Passw0rd!");
        hash.ShouldStartWith("$2");
        hash.ShouldContain("$12$");
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentSaltedHashes()
    {
        _sut.Hash("Passw0rd!").ShouldNotBe(_sut.Hash("Passw0rd!"));
    }

    [Fact]
    public void Verify_CorrectAndIncorrectPasswords()
    {
        var hash = _sut.Hash("Passw0rd!");

        _sut.Verify("Passw0rd!", hash).ShouldBeTrue();
        _sut.Verify("passw0rd!", hash).ShouldBeFalse();
        _sut.Verify("", hash).ShouldBeFalse();
    }
}
