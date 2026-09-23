using FluentAssertions;
using JournalApp.Infrastructure.Security;

namespace JournalApp.Infrastructure.Tests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_Password_DoesNotReturnPlainText()
    {
        // Act
        var hash = _sut.Hash("Password1");

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotContain("Password1");
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        // Act
        var first = _sut.Hash("Password1");
        var second = _sut.Hash("Password1");

        // Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var hash = _sut.Hash("Password1");

        // Act
        var result = _sut.Verify(hash, "Password1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        // Arrange
        var hash = _sut.Hash("Password1");

        // Act
        var result = _sut.Verify(hash, "Password2");

        // Assert
        result.Should().BeFalse();
    }
}
