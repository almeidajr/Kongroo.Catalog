using System.Net;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Kongroo.Catalog.Specs.Support;

public static class SpecsEnvironment
{
    private const string PostgreSqlImage = "postgres:18.3";
    private const string RabbitMqImage = "rabbitmq:4-management";
    private const string RabbitMqUsername = "kongroo";
    private const string RabbitMqPassword = "development";

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static PostgreSqlContainer? _database;
    private static RabbitMqContainer? _broker;
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
            _broker = new RabbitMqBuilder(RabbitMqImage)
                .WithUsername(RabbitMqUsername)
                .WithPassword(RabbitMqPassword)
                .Build();

            await Task.WhenAll(_database.StartAsync(cancellationToken), _broker.StartAsync(cancellationToken));

            _factory = new KongrooWebApplicationFactory(
                _database.GetConnectionString(),
                _broker.Hostname,
                _broker.GetMappedPublicPort(5672),
                RabbitMqUsername,
                RabbitMqPassword
            );

            await WaitForHealthyAsync(cancellationToken);
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
                "catalog"."outbox_message",
                "catalog"."order_lines",
                "catalog"."orders",
                "catalog"."ownerships",
                "catalog"."promotions",
                "catalog"."games"
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

        if (_broker is not null)
        {
            await _broker.DisposeAsync();
            _broker = null;
        }

        if (_database is not null)
        {
            await _database.DisposeAsync();
            _database = null;
        }
    }

    private static async Task WaitForHealthyAsync(CancellationToken cancellationToken)
    {
        // The MassTransit bus connects to RabbitMQ asynchronously on startup, so its
        // health check (and therefore /health) is briefly unhealthy after host start.
        using var client = Factory.CreateClient();

        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (true)
        {
            using var response = await client.GetAsync("/health", cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            if (DateTimeOffset.UtcNow >= deadline)
            {
                response.EnsureSuccessStatusCode();
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }
    }
}
