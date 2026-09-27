using EchoBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBoard.Infrastructure.Persistence.Configurations;

public sealed class SoundCategoryConfiguration : IEntityTypeConfiguration<SoundCategory>
{
    public void Configure(EntityTypeBuilder<SoundCategory> builder)
    {
        builder.ToTable("SoundCategories");
        builder.HasKey(assignment => new { assignment.SoundId, assignment.CategoryId });

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(assignment => assignment.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(assignment => new { assignment.CategoryId, assignment.SoundId });
    }
}
