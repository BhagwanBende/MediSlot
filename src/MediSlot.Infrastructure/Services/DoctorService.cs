using System.Linq.Expressions;
using MediSlot.Application.Common;
using MediSlot.Application.DTOs.Doctors;
using MediSlot.Application.Interfaces;
using MediSlot.Domain.Entities;
using MediSlot.Domain.Enums;
using MediSlot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediSlot.Infrastructure.Services;

public class DoctorService : IDoctorService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;

    public DoctorService(AppDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    // Ek ch jaagevar mapping, mhanje list ani single doctor same shape deto
    private static readonly Expression<Func<Doctor, DoctorResponse>> ToResponse = d => new DoctorResponse(
        d.Id,
        d.UserId,
        d.User.Name,
        d.User.Email,
        d.SpecializationId,
        d.Specialization.Name,
        d.ExperienceYears,
        d.Fee,
        d.User.IsActive);

    public async Task<List<DoctorResponse>> GetAllAsync(
        int? specializationId, string? search, CancellationToken ct = default)
    {
        var query = _db.Doctors.AsNoTracking().Where(d => d.User.IsActive);

        if (specializationId.HasValue)
            query = query.Where(d => d.SpecializationId == specializationId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d => d.User.Name.Contains(term));
        }

        return await query
            .OrderBy(d => d.User.Name)
            .Select(ToResponse)
            .ToListAsync(ct);
    }

    public async Task<DoctorResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Doctors
            .AsNoTracking()
            .Where(d => d.Id == id && d.User.IsActive)
            .Select(ToResponse)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Doctor not found.");
    }

    public async Task<DoctorResponse> CreateAsync(CreateDoctorRequest request, CancellationToken ct = default)
    {
        if (!await _db.Specializations.AnyAsync(s => s.Id == request.SpecializationId, ct))
            throw new BadRequestException("Specialization does not exist.");

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("Email already registered.");

        var doctor = new Doctor
        {
            User = new User
            {
                Name = request.Name.Trim(),
                Email = email,
                PasswordHash = _hasher.Hash(request.Password),
                Role = UserRole.Doctor
            },
            SpecializationId = request.SpecializationId,
            ExperienceYears = request.ExperienceYears,
            Fee = request.Fee
        };

        // User + Doctor ek ch SaveChanges (ek transaction) madhe
        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(doctor.Id, ct);
    }

    public async Task<DoctorResponse> UpdateAsync(int id, UpdateDoctorRequest request, CancellationToken ct = default)
    {
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id && d.User.IsActive, ct)
            ?? throw new NotFoundException("Doctor not found.");

        if (!await _db.Specializations.AnyAsync(s => s.Id == request.SpecializationId, ct))
            throw new BadRequestException("Specialization does not exist.");

        doctor.User.Name = request.Name.Trim();
        doctor.SpecializationId = request.SpecializationId;
        doctor.ExperienceYears = request.ExperienceYears;
        doctor.Fee = request.Fee;

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id && d.User.IsActive, ct)
            ?? throw new NotFoundException("Doctor not found.");

        // Future booked appointments asel tar deactivate nahi
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hasUpcoming = await _db.Appointments.AnyAsync(a =>
            a.DoctorId == id &&
            a.Status == AppointmentStatus.Booked &&
            a.Slot.Date >= today, ct);

        if (hasUpcoming)
            throw new ConflictException("Doctor has upcoming booked appointments. Cancel or complete them first.");

        // Delete nahi, fakt inactive (medical records chukun jat nahit)
        doctor.User.IsActive = false;

        // Doctor che sarve refresh tokens revoke
        await _db.RefreshTokens
            .Where(r => r.UserId == doctor.UserId && !r.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRevoked, true), ct);

        await _db.SaveChangesAsync(ct);
    }
}