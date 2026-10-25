namespace MediSlot.Application.DTOs.Reports;

public record AppointmentReportResponse(
    DateOnly FromDate,
    DateOnly ToDate,
    ReportSummary Summary,
    List<DoctorReportRow> Doctors);