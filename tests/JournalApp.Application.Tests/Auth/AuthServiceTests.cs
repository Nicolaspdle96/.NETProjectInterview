using FluentAssertions;
using JournalApp.Application.Auth;
using JournalApp.Application.Auth.Dtos;
using JournalApp.Application.Auth.Validators;
using JournalApp.Application.Common.Interfaces;
using JournalApp.Application.Tests.TestDoubles;
using JournalApp.Domain.Entities;
using JournalApp.Domain.Exceptions;
using Moq;

namespace JournalApp.Application.Tests.Auth;

public class AuthServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-password");
        _sut = new AuthService(
            _users.Object,
            _hasher.Object,
            _tokens.Object,
            _currentUser.Object,
            _unitOfWork.Object,
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_PersistsUserWithHashedPasswordAndReturnsDto()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "New@Journal.com", "Password1");
        User? added = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => added = user);

        // Act
        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        added.Should().NotBeNull();
        added!.PasswordHash.Should().Be("hashed-password");
        added.Email.Should().Be("new@journal.com");
        added.CreatedAt.Should().Be(Now.UtcDateTime);
        _hasher.Verify(h => h.Hash("Password1"), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeEquivalentTo(new UserDto(added.Id, "newuser", "new@journal.com", Now.UtcDateTime));
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyInUse_ThrowsConflictException()
    {
        // Arrange
        _users.Setup(u => u.ExistsByEmailAsync("taken@journal.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var request = new RegisterRequest("newuser", "TAKEN@journal.com", "Password1");

        // Act
        var act = () => _sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_UsernameAlreadyInUse_ThrowsConflictException()
    {
        // Arrange
        _users.Setup(u => u.ExistsByUsernameAsync("taken", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var request = new RegisterRequest("taken", "new@journal.com", "Password1");

        // Act
        var act = () => _sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task RegisterAsync_WeakPassword_ThrowsDomainValidationException()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "new@journal.com", "weak");

        // Act
        var act = () => _sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<DomainValidationException>())
            .Which.Errors.Should().ContainKey("Password");
        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndUser()
    {
        // Arrange
        var user = User.Create("demo", "demo@journal.com", "stored-hash", Now.UtcDateTime);
        var expiresAt = Now.UtcDateTime.AddHours(1);
        _users.Setup(u => u.GetByEmailAsync("demo@journal.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("stored-hash", "Demo123!")).Returns(true);
        _tokens.Setup(t => t.GenerateToken(user)).Returns(new AccessToken("jwt-token", expiresAt));

        // Act
        var result = await _sut.LoginAsync(new LoginRequest(" Demo@Journal.com ", "Demo123!"), CancellationToken.None);

        // Assert
        result.Token.Should().Be("jwt-token");
        result.ExpiresAt.Should().Be(expiresAt);
        result.User.Id.Should().Be(user.Id);
        result.User.Username.Should().Be("demo");
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorizedException()
    {
        // Arrange
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _sut.LoginAsync(new LoginRequest("nobody@journal.com", "Password1"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = User.Create("demo", "demo@journal.com", "stored-hash", Now.UtcDateTime);
        _users.Setup(u => u.GetByEmailAsync("demo@journal.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("stored-hash", It.IsAny<string>())).Returns(false);

        // Act
        var act = () => _sut.LoginAsync(new LoginRequest("demo@journal.com", "Wrong123"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
        _tokens.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_EmptyFields_ThrowsDomainValidationException()
    {
        // Act
        var act = () => _sut.LoginAsync(new LoginRequest("", ""), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task GetCurrentUserAsync_AuthenticatedUser_ReturnsDto()
    {
        // Arrange
        var user = User.Create("demo", "demo@journal.com", "hash", Now.UtcDateTime);
        _currentUser.Setup(c => c.UserId).Returns(user.Id);
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        var result = await _sut.GetCurrentUserAsync(CancellationToken.None);

        // Assert
        result.Id.Should().Be(user.Id);
        result.Email.Should().Be("demo@journal.com");
    }

    [Fact]
    public async Task GetCurrentUserAsync_NoAuthenticatedUser_ThrowsUnauthorizedException()
    {
        // Arrange
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);

        // Act
        var act = () => _sut.GetCurrentUserAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task GetCurrentUserAsync_UserNoLongerExists_ThrowsUnauthorizedException()
    {
        // Arrange
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _users.Setup(u => u.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _sut.GetCurrentUserAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
