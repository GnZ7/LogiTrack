using LogiTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiTrack.Data.Configurations;

internal class ItemEntityConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> entity)
    {
        entity.HasKey(i => i.ItemId);

        entity.Property(i => i.ItemId).ValueGeneratedOnAdd();

        entity.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(120);
        entity.Property(i => i.Quantity)
            .IsRequired();
        entity.Property(i => i.Location)
            .IsRequired()
            .HasMaxLength(200);        
    }
}