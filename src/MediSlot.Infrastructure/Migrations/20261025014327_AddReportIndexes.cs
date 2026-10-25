using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediSlot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Slots_DoctorId_Date_StartTime",
                table: "Slots");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Slots_Available",
                table: "Slots",
                columns: new[] { "DoctorId", "Date", "StartTime" },
                unique: true,
                filter: "[IsBooked] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Slots_Date_DoctorId",
                table: "Slots",
                columns: new[] { "Date", "DoctorId" })
                .Annotation("SqlServer:Include", new[] { "IsBooked" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_Status",
                table: "Appointments",
                columns: new[] { "DoctorId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Slots_Available",
                table: "Slots");

            migrationBuilder.DropIndex(
                name: "IX_Slots_Date_DoctorId",
                table: "Slots");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId_Status",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Slots_DoctorId_Date_StartTime",
                table: "Slots",
                columns: new[] { "DoctorId", "Date", "StartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments",
                column: "DoctorId");
        }
    }
}
