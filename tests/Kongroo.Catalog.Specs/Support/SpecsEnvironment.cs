using System.Net;
using Kongroo.Catalog.Infrastructure;
using Microsoft.Extensions.Caching.Hybrid;
using MongoDB.Driver;
using Npgsql;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace Kongroo.Catalog.Specs.Support;

public static class SpecsEnvironment
{
    private const string PostgreSqlImage = "postgres:18.3";
    private const string RabbitMqImage = "rabbitmq:4-management";
    private const string RabbitMqUsername = "kongroo";
    private const string RabbitMqPassword = "development";
    private const string MongoDbImage = "mongo:8.0";
    private const string RedisImage = "redis:8.2-alpine";

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static PostgreSqlContainer? _database;
    private static RabbitMqContainer? _broker;
    private static MongoDbContainer? _mongo;
    private static RedisContainer? _redis;
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
            _mongo = new MongoDbBuilder(MongoDbImage).Build();
            _redis = new RedisBuilder(RedisImage).Build();

            await Task.WhenAll(
                _database.StartAsync(cancellationToken),
                _broker.StartAsync(cancellationToken),
                _mongo.StartAsync(cancellationToken),
                _redis.StartAsync(cancellationToken)
            );

            _factory = new KongrooWebApplicationFactory(
                _database.GetConnectionString(),
                _broker.Hostname,
                _broker.GetMappedPublicPort(5672),
                RabbitMqUsername,
                RabbitMqPassword,
                _mongo.GetConnectionString(),
                _redis.GetConnectionString()
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

        var mongoConnectionString =
            _mongo?.GetConnectionString()
            ?? throw new InvalidOperationException("The specs MongoDB has not been started.");
        await new MongoClient(mongoConnectionString)
            .GetDatabase("kongroo_catalog_specs")
            .GetCollection<ReviewDocument>(ReviewDocument.CollectionName)
            .DeleteManyAsync(FilterDefinition<ReviewDocument>.Empty, cancellationToken);

        await Factory.Services.GetRequiredService<HybridCache>().RemoveByTagAsync("games", cancellationToken);
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

        if (_mongo is not null)
        {
            await _mongo.DisposeAsync();
            _mongo = null;
        }

        if (_redis is not null)
        {
            await _redis.DisposeAsync();
            _redis = null;
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
