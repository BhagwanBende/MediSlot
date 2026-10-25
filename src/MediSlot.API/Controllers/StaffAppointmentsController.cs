using MediSlot.Application.Common;
using MediSlot.Application.DTOs.Appointments;
using MediSlot.Application.Interfaces;
using MediSlot.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MediSlot.API.Controllers;

[ApiController]
[Authorize]
public class StaffAppointmentsController : ControllerBase
{
    private readonly IAppointmentService _service;

    public StaffAppointmentsController(IAppointmentService service)
    {
        _service = service;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Doctor: swatache appointments
    [HttpGet("api/doctors/me/appointments")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<PagedResponse<AppointmentResponse>>> GetMine(
        [FromQuery] AppointmentQuery query, CancellationToken ct)
        => Ok(await _service.GetForDoctorAsync(CurrentUserId, query, ct));

    [HttpPut("api/appointments/{id:int}/complete")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<AppointmentResponse>> Complete(int id, CancellationToken ct)
        => Ok(await _service.CompleteAsync(CurrentUserId, id, ct));

    [HttpPut("api/appointments/{id:int}/no-show")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<AppointmentResponse>> NoShow(int id, CancellationToken ct)
        => Ok(await _service.MarkNoShowAsync(CurrentUserId, id, ct));

    // Admin: sarve appointments
    [HttpGet("api/admin/appointments")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResponse<AppointmentResponse>>> GetAll(
        [FromQuery] AppointmentQuery query, CancellationToken ct)
        => Ok(await _service.GetAllAsync(query, ct));
}