using MediSlot.Application.DTOs.Reports;
using MediSlot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediSlot.API.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public class AdminReportsController : ControllerBase
{
    private readonly IReportService _service;

    public AdminReportsController(IReportService service)
    {
        _service = service;
    }

    [HttpGet("appointments")]
    public async Task<ActionResult<AppointmentReportResponse>> GetAppointmentReport(
        [FromQuery] ReportQuery query, CancellationToken ct)
        => Ok(await _service.GetAppointmentReportAsync(query, ct));
}