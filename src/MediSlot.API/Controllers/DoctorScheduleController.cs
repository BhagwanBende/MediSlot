using System.Security.Claims;
using MediSlot.Application.DTOs.Schedules;
using MediSlot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediSlot.API.Controllers;

[ApiController]
[Authorize]
public class DoctorScheduleController : ControllerBase
{
    private readonly IScheduleService _service;

    public DoctorScheduleController(IScheduleService service)
    {
        _service = service;
    }

    // Token madhla UserId (client kadhi UserId pathvat nahi, mhanje dusryacha id vaparta yet nahi)
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("api/doctors/{doctorId:int}/schedule")]
    public async Task<ActionResult<List<ScheduleResponse>>> GetForDoctor(int doctorId, CancellationToken ct)
        => Ok(await _service.GetForDoctorAsync(doctorId, ct));

    [HttpGet("api/doctors/me/schedule")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<List<ScheduleResponse>>> GetMine(CancellationToken ct)
        => Ok(await _service.GetMineAsync(CurrentUserId, ct));

    [HttpPost("api/doctors/me/schedule")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<ScheduleResponse>> Add(ScheduleRequest request, CancellationToken ct)
    {
        var created = await _service.AddAsync(CurrentUserId, request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpDelete("api/doctors/me/schedule/{scheduleId:int}")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> Delete(int scheduleId, CancellationToken ct)
    {
        await _service.DeleteAsync(CurrentUserId, scheduleId, ct);
        return NoContent();
    }
}