using MediSlot.Domain.Entities;

namespace MediSlot.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);
    string GenerateRefreshToken();
}