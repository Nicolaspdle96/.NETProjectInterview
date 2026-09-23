using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Application.Auth;
using TaskManager.Application.Common.Results;
using TaskManager.Application.Tests.Fakes;
using TaskManager.Domain.Users;

namespace TaskManager.Application.Tests.Auth;

public sealed class AuthServiceTests
{
    private const string ValidPassword = "Passw0rd!";

    private static readonly DateTime Now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TokenExpiry = Now.AddHours(1);

    private readonly InMemoryUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeCurrentUserService _currentUser = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(
            _users,
            _unitOfWork,
            _hasher,
            new FakeJwtTokenGenerator(TokenExpiry),
            _currentUser,
            new FixedDateTimeProvider(Now),
            new RegisterRequestValidator(),
            new LoginRequestValidator());
    }

    private User SeedUser(string email = "alice@example.com", string password = ValidPassword)
    {
        var user = User.Create(email, _hasher.Hash(password), Now.AddDays(-1));
        _users.Add(user);
        return user;
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesUserWithHashedPasswordAndNormalizedEmail()
    {
        var result = await _sut.RegisterAsync(new RegisterRequest(" Alice@Example.com ", ValidPassword), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var user = _users.Users.ShouldHaveSingleItem();
        result.Value.ShouldBe(new RegisterResponse(user.Id, "alice@example.com"));
        user.PasswordHash.ShouldNotBe(ValidPassword);
        _hasher.Verify(ValidPassword, user.PasswordHash).ShouldBeTrue();
        user.CreatedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task RegisterAsync_InvalidRequest_ReturnsValidationErrorAndSavesNothing()
    {
        var result = await _sut.RegisterAsync(new RegisterRequest("not-an-email", "short"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.ShouldNotBeNull();
        result.Error.ValidationErrors.Keys.ShouldBe(["Email", "Password"], ignoreOrder: true);
        _users.Users.ShouldBeEmpty();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyRegisteredInDifferentCase_ReturnsConflict()
    {
        SeedUser("alice@example.com");

        var result = await _sut.RegisterAsync(new RegisterRequest("ALICE@example.com", ValidPassword), CancellationToken.None);

        result.Error.ShouldBe(AuthErrors.EmailAlreadyRegistered);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateDetectedOnSave_ReturnsConflict()
    {
        _unitOfWork.ExceptionToThrow = new UniqueConstraintException("duplicate", new InvalidOperationException());

        var result = await _sut.RegisterAsync(new RegisterRequest("alice@example.com", ValidPassword), CancellationToken.None);

        result.Error.ShouldBe(AuthErrors.EmailAlreadyRegistered);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsBearerToken()
    {
        var user = SeedUser();

        var result = await _sut.LoginAsync(new LoginRequest("alice@example.com", ValidPassword), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new LoginResponse($"token-for-{user.Id}", "Bearer", TokenExpiry));
    }

    [Fact]
    public async Task LoginAsync_EmailInDifferentCase_Succeeds()
    {
        SeedUser("alice@example.com");

        var result = await _sut.LoginAsync(new LoginRequest("  Alice@EXAMPLE.com", ValidPassword), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsInvalidCredentials()
    {
        SeedUser();

        var result = await _sut.LoginAsync(new LoginRequest("alice@example.com", "Wr0ngPassword"), CancellationToken.None);

        result.Error.ShouldBe(AuthErrors.InvalidCredentials);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsSameErrorAsWrongPassword()
    {
        var result = await _sut.LoginAsync(new LoginRequest("nobody@example.com", ValidPassword), CancellationToken.None);

        result.Error.ShouldBe(AuthErrors.InvalidCredentials);
    }

    [Fact]
    public async Task LoginAsync_MissingFields_ReturnsValidationError()
    {
        var result = await _sut.LoginAsync(new LoginRequest("", ""), CancellationToken.None);

        result.Error.ShouldNotBeNull();
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task GetCurrentUserAsync_AuthenticatedUser_ReturnsProfile()
    {
        var user = SeedUser();
        _currentUser.UserId = user.Id;

        var result = await _sut.GetCurrentUserAsync(CancellationToken.None);

        result.Value.ShouldBe(new CurrentUserResponse(user.Id, user.Email, user.CreatedAt));
    }

    [Fact]
    public async Task GetCurrentUserAsync_Anonymous_ReturnsNotAuthenticated()
    {
        var result = await _sut.GetCurrentUserAsync(CancellationToken.None);

        result.Error.ShouldBe(AuthErrors.NotAuthenticated);
    }

    [Fact]
    public async Task GetCurrentUserAsync_UserNoLongerExists_ReturnsNotAuthenticated()
    {
        _currentUser.UserId = Guid.NewGuid();

        var result = await _sut.GetCurrentUserAsync(CancellationToken.None);

        result.Error.ShouldBe(AuthErrors.NotAuthenticated);
    }
}
