namespace MediSlot.Application.DTOs.Doctors;

public record UpdateDoctorRequest(
    string Name,
    int SpecializationId,
    int ExperienceYears,
    decimal Fee);