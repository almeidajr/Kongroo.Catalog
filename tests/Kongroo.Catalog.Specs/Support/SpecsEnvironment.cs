using Npgsql;
using Testcontainers.PostgreSql;

namespace Kongroo.Catalog.Specs.Support;

public static class SpecsEnvironment
{
    private const string PostgreSqlImage = "postgres:18.3";

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static PostgreSqlContainer? _database;
    private static KongrooWebApplicationFactory? _factory;

    public static KongrooWebApplicationFactory Factory =>
        _factory ?? throw new InvalidOperationException("The specs environment has not been started.");

    private static string ConnectionString =>
        _database?.GetConnectionString()
        ?? throw new InvalidOperationException("The specs database has not been started.");

    public static async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_factory is not null)
        {
            return;
        }

        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (_factory is not null)
            {
                return;
            }

            _database = new PostgreSqlBuilder(PostgreSqlImage).Build();
            await _database.StartAsync(cancellationToken);

            _factory = new KongrooWebApplicationFactory(_database.GetConnectionString());

            using var client = _factory.CreateClient();
            using var response = await client.GetAsync("/health", cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        const string truncateSql = """
            TRUNCATE TABLE
                "catalog"."outbox_messages",
                "catalog"."order_lines",
                "catalog"."orders",
                "catalog"."promotions",
                "catalog"."games",
                "library"."outbox_messages",
                "library"."game_ownerships"
            CASCADE;
            """;

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(truncateSql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task StopAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
            _factory = null;
        }

        if (_database is not null)
        {
            await _database.DisposeAsync();
            _database = null;
        }
    }
}
