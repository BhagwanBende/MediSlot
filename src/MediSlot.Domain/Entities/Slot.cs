namespace MediSlot.Domain.Entities;

public class Slot
{
    public int Id { get; set; }

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsBooked { get; set; }

    // Concurrency token: double booking prevent karnyasathi
    public byte[] RowVersion { get; set; } = null!;
}