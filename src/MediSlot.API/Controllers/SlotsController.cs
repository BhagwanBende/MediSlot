using System.Security.Claims;
using MediSlot.Application.DTOs.Slots;
using MediSlot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediSlot.API.Controllers;

[ApiController]
[Authorize]
public class SlotsController : ControllerBase
{
    private readonly ISlotService _service;

    public SlotsController(ISlotService service)
    {
        _service = service;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Koni pan logged-in user: doctor che available slots
    [HttpGet("api/doctors/{doctorId:int}/slots")]
    public async Task<ActionResult<List<SlotResponse>>> GetAvailable(
        int doctorId, [FromQuery] DateOnly date, CancellationToken ct)
        => Ok(await _service.GetAvailableAsync(doctorId, date, ct));

    // Doctor: swatache slots generate
    [HttpPost("api/doctors/me/slots/generate")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<GenerateSlotsResponse>> GenerateMine(
        GenerateSlotsRequest request, CancellationToken ct)
        => Ok(await _service.GenerateForUserAsync(CurrentUserId, request, ct));

    // Admin: konatya pan doctor che slots generate
    [HttpPost("api/doctors/{doctorId:int}/slots/generate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GenerateSlotsResponse>> Generate(
        int doctorId, GenerateSlotsRequest request, CancellationToken ct)
        => Ok(await _service.GenerateAsync(doctorId, request, ct));
}