using EchoBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBoard.Infrastructure.Persistence.Configurations;

public sealed class RecentlyPlayedConfiguration : IEntityTypeConfiguration<RecentlyPlayed>
{
    public void Configure(EntityTypeBuilder<RecentlyPlayed> builder)
    {
        builder.ToTable("RecentlyPlayed");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.PlayedAt)
            .HasConversion(
                value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)))
            .IsRequired();

        builder.HasOne<Sound>()
            .WithMany()
            .HasForeignKey(entry => entry.SoundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entry => entry.SoundId);
        builder.HasIndex(entry => entry.PlayedAt);
    }
}
