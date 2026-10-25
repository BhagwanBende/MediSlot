namespace MediSlot.Application.DTOs.Reports;

public record ReportSummary(
    int TotalSlots,
    int BookedSlots,
    decimal UtilizationPercent,
    int TotalAppointments,
    int CompletedCount,
    int CancelledCount,
    int NoShowCount,
    decimal Revenue);