namespace MediSlot.Domain.Entities;

public class Doctor
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int SpecializationId { get; set; }
    public Specialization Specialization { get; set; } = null!;

    public int ExperienceYears { get; set; }
    public decimal Fee { get; set; }

    public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
    public ICollection<Slot> Slots { get; set; } = new List<Slot>();
}