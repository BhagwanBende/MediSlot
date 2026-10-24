using System.Security.Claims;
using MediSlot.Application.DTOs.Appointments;
using MediSlot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediSlot.API.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize(Roles = "Patient")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _service;

    public AppointmentsController(IAppointmentService service)
    {
        _service = service;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<ActionResult<AppointmentResponse>> Book(BookAppointmentRequest request, CancellationToken ct)
    {
        var created = await _service.BookAsync(CurrentUserId, request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<AppointmentResponse>>> GetMine(CancellationToken ct)
        => Ok(await _service.GetMineAsync(CurrentUserId, ct));
}