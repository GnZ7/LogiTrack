using LogiTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiTrack.Data.Configurations;

internal class OrderItemEntityConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> entity)
    {
        entity.ToTable("OrderItem");
        entity.HasKey(item => new { item.OrderId, item.ItemId });

        entity.Property(item => item.Name)
            .IsRequired()
            .HasMaxLength(120);
        entity.Property(item => item.Quantity)
            .IsRequired();
        entity.Property(item => item.Location)
            .IsRequired()
            .HasMaxLength(200);

        entity.HasOne(item => item.Item)
            .WithMany()
            .HasForeignKey(item => item.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}