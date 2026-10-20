namespace MediSlot.Application.DTOs.Auth;

public record AuthResponse(
    int UserId,
    string Name,
    string Email,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt);