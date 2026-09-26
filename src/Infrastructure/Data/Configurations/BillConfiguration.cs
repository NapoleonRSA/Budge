using Budge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budge.Infrastructure.Data.Configurations;

public class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.Property(b => b.Name)
            .HasMaxLength(140)
            .IsRequired();

        builder.Property(b => b.Amount)
            .HasPrecision(18, 2);

        builder.HasOne(b => b.Person)
            .WithMany(p => p.Bills)
            .HasForeignKey(b => b.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.CreditFacility)
            .WithOne(f => f.PaymentBill)
            .HasForeignKey<Bill>(b => b.CreditFacilityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.CreditFacilityId)
            .IsUnique();
    }
}
