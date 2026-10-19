namespace MediSlot.Domain.Entities;

public class Patient
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? BloodGroup { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}