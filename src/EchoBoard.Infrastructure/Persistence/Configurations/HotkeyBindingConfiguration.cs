using EchoBoard.Domain.Entities;
using EchoBoard.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBoard.Infrastructure.Persistence.Configurations;

public sealed class HotkeyBindingConfiguration : IEntityTypeConfiguration<HotkeyBinding>
{
    public void Configure(EntityTypeBuilder<HotkeyBinding> builder)
    {
        builder.ToTable("HotkeyBindings");

        builder.HasKey(binding => binding.Id);

        builder.Property(binding => binding.TargetKind)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(binding => binding.GlobalCommand)
            .HasConversion<int?>();

        builder.Property(binding => binding.NormalizedKeyCombination)
            .HasMaxLength(HotkeyCombination.NormalizedTextMaxLength)
            .UseCollation("NOCASE")
            .IsRequired();

        builder.Property(binding => binding.Modifiers)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(binding => binding.PrimaryKey)
            .HasMaxLength(HotkeyCombination.PrimaryKeyMaxLength)
            .IsRequired();

        builder.Property(binding => binding.IsEnabled)
            .IsRequired();

        builder.Property(binding => binding.CreatedAt)
            .HasConversion(
                value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)))
            .IsRequired();

        builder.Property(binding => binding.UpdatedAt)
            .HasConversion(
                value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)))
            .IsRequired();

        builder.HasOne<Sound>()
            .WithMany()
            .HasForeignKey(binding => binding.SoundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(binding => binding.NormalizedKeyCombination)
            .IsUnique();

        builder.HasIndex(binding => binding.SoundId)
            .IsUnique()
            .HasFilter("SoundId IS NOT NULL");

        builder.HasIndex(binding => binding.GlobalCommand)
            .IsUnique()
            .HasFilter("GlobalCommand IS NOT NULL");
    }
}
