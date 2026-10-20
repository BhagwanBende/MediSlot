using MediSlot.Application.Common;
using MediSlot.Application.DTOs.Specializations;
using MediSlot.Application.Interfaces;
using MediSlot.Domain.Entities;
using MediSlot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediSlot.Infrastructure.Services;

public class SpecializationService : ISpecializationService
{
    private readonly AppDbContext _db;

    public SpecializationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<SpecializationResponse>> GetAllAsync(CancellationToken ct = default)
        => await _db.Specializations
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SpecializationResponse(s.Id, s.Name))
            .ToListAsync(ct);

    public async Task<SpecializationResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var s = await _db.Specializations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Specialization not found.");

        return new SpecializationResponse(s.Id, s.Name);
    }

    public async Task<SpecializationResponse> CreateAsync(SpecializationRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();

        if (await _db.Specializations.AnyAsync(s => s.Name == name, ct))
            throw new ConflictException("Specialization already exists.");

        var entity = new Specialization { Name = name };
        _db.Specializations.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new SpecializationResponse(entity.Id, entity.Name);
    }

    public async Task<SpecializationResponse> UpdateAsync(int id, SpecializationRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Specializations.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("Specialization not found.");

        var name = request.Name.Trim();

        if (await _db.Specializations.AnyAsync(s => s.Name == name && s.Id != id, ct))
            throw new ConflictException("Specialization already exists.");

        entity.Name = name;
        await _db.SaveChangesAsync(ct);

        return new SpecializationResponse(entity.Id, entity.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Specializations.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("Specialization not found.");

        // Doctors asel tar delete nahi (database ni pan Restrict kelay)
        if (await _db.Doctors.AnyAsync(d => d.SpecializationId == id, ct))
            throw new ConflictException("Cannot delete: doctors are assigned to this specialization.");

        _db.Specializations.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}