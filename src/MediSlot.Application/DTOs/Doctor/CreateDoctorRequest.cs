namespace MediSlot.Application.DTOs.Doctors;

public record CreateDoctorRequest(
    string Name,
    string Email,
    string Password,
    int SpecializationId,
    int ExperienceYears,
    decimal Fee);