using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triggered.Domain;

namespace Triggered.Infrastructure.Persistence.Configurations;

public sealed class FoodConfiguration : IEntityTypeConfiguration<Food>
{
    public void Configure(EntityTypeBuilder<Food> builder)
    {
        builder.ToTable("Foods");

        builder.HasKey(food => food.Id);

        builder.Property(food => food.Id)
            .ValueGeneratedNever();

        builder.Property(food => food.Name)
            .IsRequired();
    }
}
