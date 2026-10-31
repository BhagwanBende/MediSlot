using MediSlot.Application.Common;
using MediSlot.Application.DTOs.Auth;
using MediSlot.Application.Interfaces;
using MediSlot.Domain.Entities;
using MediSlot.Infrastructure.Services;
using MediSlot.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace MediSlot.UnitTests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly TestDatabase _testDb = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly AuthService _service;
    private int _refreshCounter;

    public AuthServiceTests()
    {
        // Fake hasher: "hashed:" + password. Plain password database madhe jat nahi he tapasayla sopa
        _hasher.Setup(h => h.Hash(It.IsAny<string>()))
               .Returns<string>(password => $"hashed:{password}");
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
               .Returns<string, string>((password, hash) => hash == $"hashed:{password}");

        // Fake tokens: refresh-token-1, refresh-token-2, ... (pratyek veli vegla)
        _tokens.Setup(t => t.GenerateAccessToken(It.IsAny<User>()))
               .Returns(("access-token", DateTime.UtcNow.AddMinutes(15)));
        _tokens.Setup(t => t.GenerateRefreshToken())
               .Returns(() => $"refresh-token-{++_refreshCounter}");

        var jwtOptions = Options.Create(new JwtSettings { RefreshTokenDays = 7 });

        _service = new AuthService(_testDb.Context, _hasher.Object, _tokens.Object, jwtOptions);
    }

    public void Dispose() => _testDb.Dispose();

    private static RegisterRequest NewRegisterRequest(string email = "patient@test.com", string password = "Test@1234")
        => new("Test Patient", email, password, new DateOnly(1996, 5, 10), "Male", "9876543210", "O+");

    // ---------- Register ----------

    [Fact]
    public async Task Register_creates_patient_with_hashed_password_and_returns_tokens()
    {
        var response = await _service.RegisterAsync(NewRegisterRequest());

        Assert.Equal("Patient", response.Role);
        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token-1", response.RefreshToken);

        var user = await _testDb.Context.Users.SingleAsync();
        Assert.Equal("hashed:Test@1234", user.PasswordHash);   // plain password nahi
        Assert.Equal(1, await _testDb.Context.Patients.CountAsync());
        Assert.Equal(1, await _testDb.Context.RefreshTokens.CountAsync());

        _hasher.Verify(h => h.Hash("Test@1234"), Times.Once);
    }

    [Fact]
    public async Task Register_trims_and_lowercases_email()
    {
        await _service.RegisterAsync(NewRegisterRequest(email: "  Patient@Test.COM "));

        var user = await _testDb.Context.Users.SingleAsync();
        Assert.Equal("patient@test.com", user.Email);
    }

    [Fact]
    public async Task Register_with_existing_email_throws_conflict_even_if_case_differs()
    {
        await _service.RegisterAsync(NewRegisterRequest("patient@test.com"));

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.RegisterAsync(NewRegisterRequest("PATIENT@test.com")));
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_with_correct_credentials_returns_tokens()
    {
        await _service.RegisterAsync(NewRegisterRequest());

        var response = await _service.LoginAsync(new LoginRequest("patient@test.com", "Test@1234"));

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("Patient", response.Role);
    }

    [Fact]
    public async Task Login_with_wrong_password_throws_unauthorized()
    {
        await _service.RegisterAsync(NewRegisterRequest());

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(new LoginRequest("patient@test.com", "Wrong@1234")));
    }

    [Fact]
    public async Task Login_unknown_email_gives_same_message_as_wrong_password()
    {
        await _service.RegisterAsync(NewRegisterRequest());

        var wrongPassword = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(new LoginRequest("patient@test.com", "Wrong@1234")));

        var unknownEmail = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(new LoginRequest("nobody@test.com", "Test@1234")));

        // Attacker la email register ahe ki nahi he kalu naye
        Assert.Equal(wrongPassword.Message, unknownEmail.Message);
    }

    [Fact]
    public async Task Login_inactive_user_throws_unauthorized()
    {
        await _service.RegisterAsync(NewRegisterRequest());

        var user = await _testDb.Context.Users.SingleAsync();
        user.IsActive = false;
        await _testDb.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(new LoginRequest("patient@test.com", "Test@1234")));
    }

    // ---------- Refresh token ----------

    [Fact]
    public async Task Refresh_rotates_the_token()
    {
        var registered = await _service.RegisterAsync(NewRegisterRequest());

        var refreshed = await _service.RefreshAsync(new RefreshTokenRequest(registered.RefreshToken));

        Assert.Equal("refresh-token-2", refreshed.RefreshToken);

        var tokens = await _testDb.Context.RefreshTokens.AsNoTracking().ToListAsync();
        Assert.True(tokens.Single(t => t.Token == "refresh-token-1").IsRevoked);    // junya cha vapar sampla
        Assert.False(tokens.Single(t => t.Token == "refresh-token-2").IsRevoked);   // navin chalu
    }

    [Fact]
    public async Task Reusing_an_old_refresh_token_revokes_all_tokens_and_throws()
    {
        var registered = await _service.RegisterAsync(NewRegisterRequest());
        await _service.RefreshAsync(new RefreshTokenRequest(registered.RefreshToken));

        // Junya (aadhich vaparlelya) token cha parat vapar = chori zali asavi
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.RefreshAsync(new RefreshTokenRequest(registered.RefreshToken)));

        var tokens = await _testDb.Context.RefreshTokens.AsNoTracking().ToListAsync();
        Assert.All(tokens, t => Assert.True(t.IsRevoked));
    }

    [Fact]
    public async Task Refresh_with_expired_token_throws_unauthorized()
    {
        var registered = await _service.RegisterAsync(NewRegisterRequest());

        var stored = await _testDb.Context.RefreshTokens.SingleAsync();
        stored.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await _testDb.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.RefreshAsync(new RefreshTokenRequest(registered.RefreshToken)));
    }

    [Fact]
    public async Task Refresh_with_unknown_token_throws_unauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.RefreshAsync(new RefreshTokenRequest("does-not-exist")));
    }

    // ---------- Logout ----------

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var registered = await _service.RegisterAsync(NewRegisterRequest());

        await _service.LogoutAsync(new RefreshTokenRequest(registered.RefreshToken));

        var stored = await _testDb.Context.RefreshTokens.AsNoTracking().SingleAsync();
        Assert.True(stored.IsRevoked);
    }

    [Fact]
    public async Task Logout_with_unknown_token_does_not_throw()
    {
        var exception = await Record.ExceptionAsync(() =>
            _service.LogoutAsync(new RefreshTokenRequest("does-not-exist")));

        Assert.Null(exception);
    }
}