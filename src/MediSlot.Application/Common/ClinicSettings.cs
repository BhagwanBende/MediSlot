namespace MediSlot.Application.Common;

public class ClinicSettings
{
    public const string SectionName = "Clinic";

    public string TimeZoneId { get; set; } = "UTC";
    public int MaxGenerateDays { get; set; } = 30;
}