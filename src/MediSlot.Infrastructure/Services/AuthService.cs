using MediSlot.Application.Common;
using MediSlot.Application.DTOs.Auth;
using MediSlot.Application.Interfaces;
using MediSlot.Domain.Entities;
using MediSlot.Domain.Enums;
using MediSlot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MediSlot.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly JwtSettings _jwt;

    public AuthService(
        AppDbContext db,
        IPasswordHasher hasher,
        ITokenService tokens,
        IOptions<JwtSettings> jwtOptions)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _jwt = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("Email already registered.");

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            Role = UserRole.Patient
        };

        var patient = new Patient
        {
            User = user,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Phone = request.Phone,
            BloodGroup = request.BloodGroup
        };

        // Patient add kela ki User pan aapoaap insert hoto, ek ch transaction madhe
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Email chukla ki password chukla, donhi sathi same message (attacker la hint nako)
        if (user is null || !user.IsActive || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var stored = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, ct);

        if (stored is null)
            throw new UnauthorizedException("Invalid refresh token.");

        // Already vaparlela token parat aala = chori zali asavi, sarve tokens revoke
        if (stored.IsRevoked)
        {
            await RevokeAllAsync(stored.UserId, ct);
            throw new UnauthorizedException("Refresh token reuse detected. Please login again.");
        }

        if (stored.ExpiresAt <= DateTime.UtcNow || !stored.User.IsActive)
            throw new UnauthorizedException("Refresh token expired.");

        // Rotation: junya token cha vapar sampla, navin deto
        stored.IsRevoked = true;

        return await IssueTokensAsync(stored.User, ct);
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken && !r.IsRevoked, ct);

        if (stored is null) return;

        stored.IsRevoked = true;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var (accessToken, expiresAt) = _tokens.GenerateAccessToken(user);
        var refreshToken = _tokens.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays)
        });

        await _db.SaveChangesAsync(ct);

        return new AuthResponse(
            user.Id,
            user.Name,
            user.Email,
            user.Role.ToString(),
            accessToken,
            refreshToken,
            expiresAt);
    }

    private async Task RevokeAllAsync(int userId, CancellationToken ct)
    {
        await _db.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRevoked, true), ct);
    }
}