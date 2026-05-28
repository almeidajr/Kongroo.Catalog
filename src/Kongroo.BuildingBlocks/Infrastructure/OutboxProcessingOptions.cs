using System.ComponentModel.DataAnnotations;

namespace Kongroo.BuildingBlocks.Infrastructure;

public sealed class OutboxProcessingOptions
{
    public const string SectionName = "OutboxProcessing";

    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    [Range(1, 1000)]
    public int BatchSize { get; set; } = 20;
}
