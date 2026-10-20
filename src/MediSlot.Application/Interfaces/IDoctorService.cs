using MediSlot.Application.DTOs.Doctors;

namespace MediSlot.Application.Interfaces;

public interface IDoctorService
{
    Task<List<DoctorResponse>> GetAllAsync(int? specializationId, string? search, CancellationToken ct = default);
    Task<DoctorResponse> GetByIdAsync(int id, CancellationToken ct = default);
    Task<DoctorResponse> CreateAsync(CreateDoctorRequest request, CancellationToken ct = default);
    Task<DoctorResponse> UpdateAsync(int id, UpdateDoctorRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
}