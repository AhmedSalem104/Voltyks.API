using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voltyks.Persistence.Entities.Main.Store;

namespace Voltyks.Persistence.Data.Configurations.Store
{
    public class StoreReservationConfiguration : IEntityTypeConfiguration<StoreReservation>
    {
        public void Configure(EntityTypeBuilder<StoreReservation> builder)
        {
            builder.ToTable("StoreReservations");

            builder.Property(r => r.UserId)
                   .IsRequired()
                   .HasMaxLength(450);

            builder.Property(r => r.Quantity)
                   .IsRequired()
                   .HasDefaultValue(1);

            builder.Property(r => r.UnitPrice)
                   .IsRequired()
                   .HasColumnType("decimal(18,2)");

            builder.Property(r => r.TotalPrice)
                   .IsRequired()
                   .HasColumnType("decimal(18,2)");

            builder.Property(r => r.Status)
                   .IsRequired()
                   .HasMaxLength(20)
                   .HasDefaultValue("pending");

            builder.Property(r => r.PaymentStatus)
                   .IsRequired()
                   .HasMaxLength(20)
                   .HasDefaultValue("unpaid");

            builder.Property(r => r.PaymentMethod)
                   .HasMaxLength(50);

            builder.Property(r => r.PaymentReference)
                   .HasMaxLength(100);

            builder.Property(r => r.PaidAmount)
                   .HasColumnType("decimal(18,2)");

            builder.Property(r => r.DeliveryStatus)
                   .IsRequired()
                   .HasMaxLength(20)
                   .HasDefaultValue("pending");

            builder.Property(r => r.DeliveryNotes)
                   .HasMaxLength(1000);

            builder.Property(r => r.AdminNotes)
                   .HasMaxLength(1000);

            // Relationships
            builder.HasOne(r => r.User)
                   .WithMany()
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Product)
                   .WithMany(p => p.Reservations)
                   .HasForeignKey(r => r.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(r => new { r.UserId, r.ProductId })
                   .IsUnique()
                   .HasFilter("[Status] != 'cancelled'")
                   .HasDatabaseName("IX_StoreReservations_UserId_ProductId_Active");

            builder.HasIndex(r => new { r.Status, r.PaymentStatus, r.DeliveryStatus })
                   .HasDatabaseName("IX_StoreReservations_Status_PaymentStatus_DeliveryStatus");

            builder.HasIndex(r => r.CreatedAt)
                   .HasDatabaseName("IX_StoreReservations_CreatedAt");
        }
    }
}
