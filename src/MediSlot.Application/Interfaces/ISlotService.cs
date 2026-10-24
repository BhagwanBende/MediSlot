using MediSlot.Application.DTOs.Slots;

namespace MediSlot.Application.Interfaces;

public interface ISlotService
{
    Task<GenerateSlotsResponse> GenerateAsync(int doctorId, GenerateSlotsRequest request, CancellationToken ct = default);
    Task<GenerateSlotsResponse> GenerateForUserAsync(int userId, GenerateSlotsRequest request, CancellationToken ct = default);
    Task<List<SlotResponse>> GetAvailableAsync(int doctorId, DateOnly date, CancellationToken ct = default);
}