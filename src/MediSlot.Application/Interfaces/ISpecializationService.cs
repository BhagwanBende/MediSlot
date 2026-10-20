using MediSlot.Application.DTOs.Specializations;

namespace MediSlot.Application.Interfaces;

public interface ISpecializationService
{
    Task<List<SpecializationResponse>> GetAllAsync(CancellationToken ct = default);
    Task<SpecializationResponse> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SpecializationResponse> CreateAsync(SpecializationRequest request, CancellationToken ct = default);
    Task<SpecializationResponse> UpdateAsync(int id, SpecializationRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}