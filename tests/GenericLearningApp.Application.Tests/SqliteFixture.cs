using GenericLearningApp.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GenericLearningApp.Application.Tests;

/// <summary>
/// A real relational database per test, held open in memory. SQLite rather than the in-memory
/// provider because the model uses relational mapping (tables, column types) that the
/// in-memory provider rejects.
/// </summary>
public sealed class SqliteFixture : IDisposable, IDbContextFactory<LearningDbContext>
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<LearningDbContext> _options;

    public SqliteFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<LearningDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    public LearningDbContext CreateDbContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
