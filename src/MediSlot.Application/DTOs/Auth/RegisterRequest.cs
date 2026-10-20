namespace MediSlot.Application.DTOs.Auth;

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    DateOnly DateOfBirth,
    string Gender,
    string Phone,
    string? BloodGroup);