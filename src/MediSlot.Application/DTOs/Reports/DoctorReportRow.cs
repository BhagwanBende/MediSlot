namespace MediSlot.Application.DTOs.Reports;

// Property che nav stored procedure cha column names shi exactly same pahijet
public class DoctorReportRow
{
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public int TotalSlots { get; set; }
    public int BookedSlots { get; set; }
    public decimal? UtilizationPercent { get; set; }
    public int TotalAppointments { get; set; }
    public int BookedCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int NoShowCount { get; set; }
    public decimal Revenue { get; set; }
    public long CompletedRank { get; set; }   // RANK() bigint return karto
}