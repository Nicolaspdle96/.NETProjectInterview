using FluentAssertions;
using JournalApp.Application.Auth;
using JournalApp.Application.Auth.Dtos;
using JournalApp.Application.Common.Interfaces;
using JournalApp.Application.Tests.TestDoubles;
using JournalApp.Domain.Exceptions;
using Moq;

namespace JournalApp.Application.Tests.Auth;

public class CachedAuthServiceTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<IAuthService> _inner = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly InMemoryCacheService _cache = new();
    private readonly CachedAuthService _sut;

    public CachedAuthServiceTests()
    {
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _inner.Setup(s => s.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserDto(_userId, "demo", "demo@journal.com", Now));
        _sut = new CachedAuthService(_inner.Object, _cache, _currentUser.Object);
    }

    [Fact]
    public async Task GetCurrentUserAsync_CalledTwice_QueriesInnerServiceOnce()
    {
        // Act
        await _sut.GetCurrentUserAsync(CancellationToken.None);
        var result = await _sut.GetCurrentUserAsync(CancellationToken.None);

        // Assert
        result.Id.Should().Be(_userId);
        _inner.Verify(s => s.GetCurrentUserAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserAsync_NoAuthenticatedUser_ThrowsUnauthorizedExceptionWithoutCaching()
    {
        // Arrange
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);

        // Act
        var act = () => _sut.GetCurrentUserAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
        _cache.Keys.Should().BeEmpty();
    }

    [Fact]
    public async Task LoginAsync_Always_DelegatesToInnerServiceWithoutCaching()
    {
        // Arrange
        var request = new LoginRequest("demo@journal.com", "Demo123!");
        var response = new AuthResponse("jwt", Now, new UserDto(_userId, "demo", "demo@journal.com", Now));
        _inner.Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        // Act
        await _sut.LoginAsync(request, CancellationToken.None);
        var result = await _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        result.Should().Be(response);
        _inner.Verify(s => s.LoginAsync(request, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _cache.Keys.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_Always_DelegatesToInnerService()
    {
        // Arrange
        var request = new RegisterRequest("newuser", "new@journal.com", "Password1");
        var user = new UserDto(Guid.NewGuid(), "newuser", "new@journal.com", Now);
        _inner.Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        result.Should().Be(user);
        _inner.Verify(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
