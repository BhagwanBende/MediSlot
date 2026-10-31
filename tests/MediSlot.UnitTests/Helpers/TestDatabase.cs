using MediSlot.Domain.Entities;
using MediSlot.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MediSlot.UnitTests.Helpers;

// Pratyek test sathi navin, rikami in-memory database (tests ekmekanna disturb karat nahit)
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public AppDbContext Context { get; }

    public TestDatabase()
    {
        // Connection open thevaychi, nahi tar in-memory database band hote
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new SqliteTestDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }

    // SQLite rowversion aapoaap generate karat nahi, mhanun tests madhe te normal column asto.
    // (Real concurrency race SQL Server var ch test hoto, ha limitation README madhe liha)
    private sealed class SqliteTestDbContext : AppDbContext
    {
        public SqliteTestDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<Slot>().Property(x => x.RowVersion)
                .IsConcurrencyToken(false)
                .ValueGeneratedNever();
        }
    }
}