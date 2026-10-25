namespace MediSlot.Application.DTOs.Doctors;

public class DoctorQuery
{
    public int? SpecializationId { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; }       // name | fee | experience
    public bool Descending { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}