using System.Data;
using MediSlot.Application.Common;
using MediSlot.Application.DTOs.Reports;
using MediSlot.Application.Interfaces;
using MediSlot.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MediSlot.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AppointmentReportResponse> GetAppointmentReportAsync(
        ReportQuery query, CancellationToken ct = default)
    {
        if (query.DoctorId.HasValue &&
            !await _db.Doctors.AnyAsync(d => d.Id == query.DoctorId.Value, ct))
            throw new NotFoundException("Doctor not found.");

        // Parameterized: user cha input kadhi SQL string madhe jodat nahi (SQL injection nahi)
        var from = new SqlParameter("@FromDate", SqlDbType.Date)
        {
            Value = query.FromDate.ToDateTime(TimeOnly.MinValue)
        };
        var to = new SqlParameter("@ToDate", SqlDbType.Date)
        {
            Value = query.ToDate.ToDateTime(TimeOnly.MinValue)
        };
        var doctorId = new SqlParameter("@DoctorId", SqlDbType.Int)
        {
            Value = (object?)query.DoctorId ?? DBNull.Value
        };

        // EXEC var kahi LINQ (OrderBy/Where) lavu naka, directly ToListAsync
        var rows = await _db.Database
            .SqlQueryRaw<DoctorReportRow>(
                "EXEC dbo.usp_GetAppointmentReport @FromDate, @ToDate, @DoctorId",
                from, to, doctorId)
            .ToListAsync(ct);

        var totalSlots = rows.Sum(r => r.TotalSlots);
        var bookedSlots = rows.Sum(r => r.BookedSlots);

        var summary = new ReportSummary(
            totalSlots,
            bookedSlots,
            totalSlots == 0 ? 0 : Math.Round(100m * bookedSlots / totalSlots, 2),
            rows.Sum(r => r.TotalAppointments),
            rows.Sum(r => r.CompletedCount),
            rows.Sum(r => r.CancelledCount),
            rows.Sum(r => r.NoShowCount),
            rows.Sum(r => r.Revenue));

        return new AppointmentReportResponse(query.FromDate, query.ToDate, summary, rows);
    }
}