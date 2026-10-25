using MediSlot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediSlot.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Specialization> Specializations => Set<Specialization>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<Slot> Slots => Set<Slot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Specialization>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<Doctor>(e =>
        {
            e.Property(x => x.Fee).HasPrecision(10, 2);
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Specialization).WithMany(s => s.Doctors)
                .HasForeignKey(x => x.SpecializationId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Patient>(e =>
        {
            e.Property(x => x.Gender).HasMaxLength(20);
            e.Property(x => x.Phone).HasMaxLength(15);
            e.Property(x => x.BloodGroup).HasMaxLength(5);
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<DoctorSchedule>(e =>
        {
            e.HasOne(x => x.Doctor).WithMany(d => d.Schedules)
                .HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Slot>(e =>
        {
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne(x => x.Doctor).WithMany(d => d.Slots)
                .HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
            // same doctor + same date + same start time = fakt ek slot
            e.HasIndex(x => new { x.DoctorId, x.Date, x.StartTime }).IsUnique();

            e.HasIndex(x => new { x.Date, x.DoctorId })
                .IncludeProperties(x => x.IsBooked)
                .HasDatabaseName("IX_Slots_Date_DoctorId");

            e.HasIndex(x => new { x.DoctorId, x.Date, x.StartTime })
                .HasFilter("[IsBooked] = 0")
                .HasDatabaseName("IX_Slots_Available");
        });

        b.Entity<Appointment>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.HasOne(x => x.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Doctor).WithMany()
                .HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Slot).WithMany()
                .HasForeignKey(x => x.SlotId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PatientId, x.Status });
            e.HasIndex(x => x.SlotId);
            e.HasIndex(x => new { x.DoctorId, x.Status });
        });

        b.Entity<RefreshToken>(e =>
        {
            e.Property(x => x.Token).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Token).IsUnique();
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}