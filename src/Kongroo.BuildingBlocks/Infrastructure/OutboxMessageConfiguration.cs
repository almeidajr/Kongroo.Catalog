using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kongroo.BuildingBlocks.Infrastructure;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).HasConversion(id => id.Value, value => OutboxMessageId.From(value));

        builder.Property(message => message.EventType).HasMaxLength(OutboxMessage.EventTypeMaxLength);
        builder.Property(message => message.Error).HasMaxLength(OutboxMessage.ErrorMaxLength);
        builder.Property(message => message.Payload).HasColumnType("jsonb");
    }
}
