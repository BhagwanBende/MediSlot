using MediSlot.Application.Common;
using MediSlot.Application.Interfaces;
using MediSlot.Domain.Entities;
using MediSlot.Domain.Enums;
using MediSlot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediSlot.Infrastructure.Services;

public class AdminSeeder
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly AdminSettings _settings;
    private readonly ILogger<AdminSeeder> _logger;

    public AdminSeeder(
        AppDbContext db,
        IPasswordHasher hasher,
        IOptions<AdminSettings> options,
        ILogger<AdminSeeder> logger)
    {
        _db = db;
        _hasher = hasher;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        // Admin aadhich asel tar kahi karaychi garaj nahi
        if (await _db.Users.AnyAsync(u => u.Role == UserRole.Admin))
            return;

        if (string.IsNullOrWhiteSpace(_settings.Email) || string.IsNullOrWhiteSpace(_settings.Password))
        {
            _logger.LogWarning("Admin credentials are not configured. Skipping admin seeding.");
            return;
        }

        _db.Users.Add(new User
        {
            Name = string.IsNullOrWhiteSpace(_settings.Name) ? "Admin" : _settings.Name.Trim(),
            Email = _settings.Email.Trim().ToLowerInvariant(),
            PasswordHash = _hasher.Hash(_settings.Password),
            Role = UserRole.Admin
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("Admin account created.");
    }
}