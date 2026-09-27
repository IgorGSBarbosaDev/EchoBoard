using EchoBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBoard.Infrastructure.Persistence.Configurations;

public sealed class SoundConfiguration : IEntityTypeConfiguration<Sound>
{
    public void Configure(EntityTypeBuilder<Sound> builder)
    {
        builder.ToTable("Sounds");

        builder.HasKey(sound => sound.Id);

        builder.Property(sound => sound.Name)
            .HasMaxLength(Sound.NameMaxLength)
            .IsRequired();

        builder.Property(sound => sound.FilePath)
            .HasMaxLength(Sound.FilePathMaxLength)
            .UseCollation("NOCASE")
            .IsRequired();

        builder.Property(sound => sound.Extension)
            .HasMaxLength(Sound.ExtensionMaxLength)
            .IsRequired();

        builder.Property(sound => sound.Duration)
            .HasConversion(
                value => value.Ticks,
                value => TimeSpan.FromTicks(value))
            .IsRequired();

        builder.Property(sound => sound.FileSize)
            .IsRequired();

        builder.Property(sound => sound.Volume)
            .IsRequired();

        builder.Property(sound => sound.IsFavorite)
            .IsRequired();

        builder.Property(sound => sound.IsLoopEnabled)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(sound => sound.StopPreviousSound)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(sound => sound.AllowOverlap)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(sound => sound.WaveformPeaks)
            .IsRequired();

        builder.Property(sound => sound.SortOrder)
            .IsRequired();

        builder.Property(sound => sound.CreatedAt)
            .HasConversion(
                value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)))
            .IsRequired();

        builder.Property(sound => sound.UpdatedAt)
            .HasConversion(
                value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)))
            .IsRequired();

        builder.HasMany(sound => sound.CategoryAssignments)
            .WithOne()
            .HasForeignKey(assignment => assignment.SoundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(sound => sound.CategoryAssignments)
            .HasField("categoryAssignments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(sound => sound.CategoryIds);

        builder.HasIndex(sound => sound.FilePath)
            .IsUnique();

        builder.HasIndex(sound => sound.IsFavorite);
    }
}
