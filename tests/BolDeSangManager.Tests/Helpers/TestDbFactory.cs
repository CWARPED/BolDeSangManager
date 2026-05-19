using BolDeSangManager.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BolDeSangManager.Tests.Helpers;

/// <summary>
/// Maintient une connexion SQLite :memory: ouverte pour la durée d'un test class.
/// Chaque appel à CreateContext() retourne un nouveau DbContext partagant la même base.
/// </summary>
public sealed class TestDbFactory : IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _schemaCreated;

    public TestDbFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public ApplicationDbContext CreateContext()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        var ctx = new ApplicationDbContext(opts);

        if (!_schemaCreated)
        {
            ctx.Database.EnsureCreated();
            _schemaCreated = true;
        }

        return ctx;
    }

    public void Dispose() => _connection.Dispose();
}
