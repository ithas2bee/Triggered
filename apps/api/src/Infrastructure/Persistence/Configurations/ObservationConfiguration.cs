using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triggered.Domain;

namespace Triggered.Infrastructure.Persistence.Configurations;

public sealed class ObservationConfiguration : IEntityTypeConfiguration<Observation>
{
    public void Configure(EntityTypeBuilder<Observation> builder)
    {
        builder.ToTable("Observations");

        builder.HasKey(observation => observation.Id);

        builder.Property(observation => observation.Id)
            .ValueGeneratedNever();

        builder.Property(observation => observation.ConsumedAt)
            .IsRequired();

        builder.Property(observation => observation.Reaction)
            .IsRequired();

        builder.HasOne(observation => observation.Food)
            .WithMany()
            .HasForeignKey(observation => observation.FoodId)
            .IsRequired();
    }
}
