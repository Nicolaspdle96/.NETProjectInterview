using TaskManager.Domain.Common;
using TaskManager.Domain.Users;

namespace TaskManager.Domain.Tests;

public sealed class UserTests
{
    private static readonly DateTime Now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var user = User.Create("alice@example.com", "hash", Now);

        user.Id.ShouldNotBe(Guid.Empty);
        user.Email.ShouldBe("alice@example.com");
        user.PasswordHash.ShouldBe("hash");
        user.CreatedAt.ShouldBe(Now);
        user.Tasks.ShouldBeEmpty();
    }

    [Fact]
    public void Create_MixedCaseEmailWithWhitespace_StoresTrimmedLowercase()
    {
        User.Create("  Alice@Example.COM ", "hash", Now).Email.ShouldBe("alice@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_MissingEmail_Throws(string? email)
    {
        Should.Throw<DomainException>(() => User.Create(email!, "hash", Now));
    }

    [Fact]
    public void Create_EmailOverMaxLength_Throws()
    {
        var email = new string('a', User.EmailMaxLength - "@x.io".Length + 1) + "@x.io";

        Should.Throw<DomainException>(() => User.Create(email, "hash", Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_MissingPasswordHash_Throws(string? hash)
    {
        Should.Throw<DomainException>(() => User.Create("alice@example.com", hash!, Now));
    }

    [Fact]
    public void Create_NonUtcNow_Throws()
    {
        Should.Throw<DomainException>(
            () => User.Create("alice@example.com", "hash", DateTime.SpecifyKind(Now, DateTimeKind.Local)));
    }
}
