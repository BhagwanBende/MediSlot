namespace MediSlot.Application.DTOs.Reports;

public class ReportQuery
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int? DoctorId { get; set; }
}