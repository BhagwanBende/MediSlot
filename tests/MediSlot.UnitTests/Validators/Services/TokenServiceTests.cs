using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MediSlot.Application.Common;
using MediSlot.Domain.Entities;
using MediSlot.Domain.Enums;
using MediSlot.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MediSlot.UnitTests.Services;

public class TokenServiceTests
{
    private const string TestKey = "unit-test-key-that-is-at-least-32-characters-long";
    private const string Issuer = "MediSlot.Tests";
    private const string Audience = "MediSlot.Tests.Client";

    private static TokenService CreateService(int accessTokenMinutes = 15, string key = TestKey)
        => new(Options.Create(new JwtSettings
        {
            Key = key,
            Issuer = Issuer,
            Audience = Audience,
            AccessTokenMinutes = accessTokenMinutes,
            RefreshTokenDays = 7
        }));

    private static User TestUser() => new()
    {
        Id = 42,
        Name = "Test Patient",
        Email = "patient@test.com",
        Role = UserRole.Patient
    };

    private static TokenValidationParameters ValidationParameters(string key) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    [Fact]
    public void Access_token_contains_user_details_issuer_and_audience()
    {
        var (token, _) = CreateService().GenerateAccessToken(TestUser());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Value == "42");
        Assert.Contains(jwt.Claims, c => c.Value == "patient@test.com");
        Assert.Contains(jwt.Claims, c => c.Value == "Patient");
        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Contains(Audience, jwt.Audiences);
    }

    [Fact]
    public void Access_token_expires_after_configured_minutes()
    {
        var (_, expiresAt) = CreateService(accessTokenMinutes: 15).GenerateAccessToken(TestUser());

        Assert.InRange(expiresAt, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public void Access_token_is_valid_with_the_correct_key()
    {
        var (token, _) = CreateService().GenerateAccessToken(TestUser());

        var principal = new JwtSecurityTokenHandler()
            .ValidateToken(token, ValidationParameters(TestKey), out _);

        Assert.NotNull(principal);
    }

    [Fact]
    public void Access_token_is_rejected_with_a_different_key()
    {
        var (token, _) = CreateService().GenerateAccessToken(TestUser());
        var wrongKey = "a-completely-different-key-with-32-plus-characters";

        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, ValidationParameters(wrongKey), out _));
    }

    [Fact]
    public void Expired_access_token_is_rejected()
    {
        var (token, _) = CreateService(accessTokenMinutes: -5).GenerateAccessToken(TestUser());

        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, ValidationParameters(TestKey), out _));
    }

    [Fact]
    public void Refresh_tokens_are_random_and_unique()
    {
        var service = CreateService();

        var first = service.GenerateRefreshToken();
        var second = service.GenerateRefreshToken();

        Assert.NotEqual(first, second);
        Assert.Equal(64, Convert.FromBase64String(first).Length);   // 64 random bytes
    }
}