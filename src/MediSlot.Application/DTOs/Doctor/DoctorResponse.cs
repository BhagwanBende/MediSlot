namespace MediSlot.Application.DTOs.Doctors;

public record DoctorResponse(
    int Id,
    int UserId,
    string Name,
    string Email,
    int SpecializationId,
    string Specialization,
    int ExperienceYears,
    decimal Fee,
    bool IsActive);