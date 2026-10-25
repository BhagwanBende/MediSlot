using MediSlot.Application.Common;
using MediSlot.Application.DTOs.Doctors;
using MediSlot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediSlot.API.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _service;

    public DoctorsController(IDoctorService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<DoctorResponse>>> GetAll(
        [FromQuery] int? specializationId,
        [FromQuery] string? search,
        CancellationToken ct)
        => Ok(await _service.GetAllAsync(specializationId, search, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DoctorResponse>> GetById(int id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DoctorResponse>> Create(CreateDoctorRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DoctorResponse>> Update(int id, UpdateDoctorRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _service.DeactivateAsync(id, ct);
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<DoctorResponse>>> GetAll(
    [FromQuery] DoctorQuery query, CancellationToken ct)
    => Ok(await _service.GetAllAsync(query, ct));
}