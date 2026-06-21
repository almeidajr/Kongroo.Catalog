using Kongroo.BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Kongroo.Catalog.UnitTests.BuildingBlocks.Infrastructure;

public sealed class DbInitializerTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task IsEnabledAsync_RegardlessOfEnvironment_ReturnsTrue(string environmentName)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);

        using var context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().Options);
        var initializer = new DbInitializer<TestDbContext>(
            environment,
            context,
            new SpyLogger<DbInitializer<TestDbContext>>()
        );

        var enabled = await initializer.IsEnabledAsync(TestContext.Current.CancellationToken);

        enabled.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task InitializeAsync_WhenNotDevelopment_LogsWarning(string environmentName)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        var logger = new SpyLogger<DbInitializer<TestDbContext>>();

        using var context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().Options);
        var initializer = new DbInitializer<TestDbContext>(environment, context, logger);

        await Should.ThrowAsync<Exception>(() => initializer.InitializeAsync(TestContext.Current.CancellationToken));

        logger.WarningCount.ShouldBe(1);
    }

    [Fact]
    public async Task InitializeAsync_InDevelopment_DoesNotLogWarning()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Development");
        var logger = new SpyLogger<DbInitializer<TestDbContext>>();

        using var context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().Options);
        var initializer = new DbInitializer<TestDbContext>(environment, context, logger);

        await Should.ThrowAsync<Exception>(() => initializer.InitializeAsync(TestContext.Current.CancellationToken));

        logger.WarningCount.ShouldBe(0);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);

    private sealed class SpyLogger<T> : ILogger<T>
    {
        public int WarningCount { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            if (logLevel == LogLevel.Warning)
            {
                WarningCount++;
            }
        }
    }
}
