using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediSlot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentReportProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.usp_GetAppointmentReport
                    @FromDate DATE,
                    @ToDate   DATE,
                    @DoctorId INT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    WITH SlotStats AS (
                        SELECT s.DoctorId,
                               COUNT(*) AS TotalSlots,
                               SUM(CASE WHEN s.IsBooked = 1 THEN 1 ELSE 0 END) AS BookedSlots
                        FROM dbo.Slots s
                        WHERE s.[Date] BETWEEN @FromDate AND @ToDate
                          AND (@DoctorId IS NULL OR s.DoctorId = @DoctorId)
                        GROUP BY s.DoctorId
                    ),
                    AppointmentStats AS (
                        SELECT a.DoctorId,
                               COUNT(*) AS TotalAppointments,
                               SUM(CASE WHEN a.Status = 'Booked'    THEN 1 ELSE 0 END) AS BookedCount,
                               SUM(CASE WHEN a.Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedCount,
                               SUM(CASE WHEN a.Status = 'Cancelled' THEN 1 ELSE 0 END) AS CancelledCount,
                               SUM(CASE WHEN a.Status = 'NoShow'    THEN 1 ELSE 0 END) AS NoShowCount
                        FROM dbo.Appointments a
                        INNER JOIN dbo.Slots s ON s.Id = a.SlotId
                        WHERE s.[Date] BETWEEN @FromDate AND @ToDate
                          AND (@DoctorId IS NULL OR a.DoctorId = @DoctorId)
                        GROUP BY a.DoctorId
                    )
                    SELECT d.Id                         AS DoctorId,
                           u.Name                       AS DoctorName,
                           sp.Name                      AS Specialization,
                           ISNULL(ss.TotalSlots, 0)     AS TotalSlots,
                           ISNULL(ss.BookedSlots, 0)    AS BookedSlots,
                           CAST(100.0 * ISNULL(ss.BookedSlots, 0) / NULLIF(ss.TotalSlots, 0) AS DECIMAL(5,2)) AS UtilizationPercent,
                           ISNULL(ap.TotalAppointments, 0) AS TotalAppointments,
                           ISNULL(ap.BookedCount, 0)    AS BookedCount,
                           ISNULL(ap.CompletedCount, 0) AS CompletedCount,
                           ISNULL(ap.CancelledCount, 0) AS CancelledCount,
                           ISNULL(ap.NoShowCount, 0)    AS NoShowCount,
                           CAST(ISNULL(ap.CompletedCount, 0) * d.Fee AS DECIMAL(12,2)) AS Revenue,
                           RANK() OVER (ORDER BY ISNULL(ap.CompletedCount, 0) DESC) AS CompletedRank
                    FROM dbo.Doctors d
                    INNER JOIN dbo.Users u            ON u.Id = d.UserId
                    INNER JOIN dbo.Specializations sp ON sp.Id = d.SpecializationId
                    LEFT JOIN SlotStats ss            ON ss.DoctorId = d.Id
                    LEFT JOIN AppointmentStats ap     ON ap.DoctorId = d.Id
                    WHERE ss.DoctorId IS NOT NULL OR ap.DoctorId IS NOT NULL
                    ORDER BY CompletedRank, u.Name;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetAppointmentReport;");
        }
    }
}