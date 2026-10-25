using MediSlot.Domain.Enums;

namespace MediSlot.Application.DTOs.Appointments;

public class AppointmentQuery
{
    public DateOnly? Date { get; set; }
    public AppointmentStatus? Status { get; set; }

    // Fakt Admin endpoint var vaparla jato. Patient/Doctor la swatacha data ch milto
    public int? DoctorId { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}