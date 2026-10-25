using MediSlot.Application.DTOs.Reports;

namespace MediSlot.Application.Interfaces;

public interface IReportService
{
    Task<AppointmentReportResponse> GetAppointmentReportAsync(ReportQuery query, CancellationToken ct = default);
}