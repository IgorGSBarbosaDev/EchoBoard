using EchoBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBoard.Infrastructure.Persistence.Configurations;

public sealed class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("AppSettings");

        builder.HasKey(setting => setting.Key);

        builder.Property(setting => setting.Key)
            .HasMaxLength(AppSetting.KeyMaxLength)
            .IsRequired();

        builder.Property(setting => setting.Value)
            .HasMaxLength(AppSetting.ValueMaxLength)
            .IsRequired();

        builder.Property(setting => setting.UpdatedAt)
            .HasConversion(
                value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)))
            .IsRequired();
    }
}
