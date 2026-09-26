using Budge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budge.Infrastructure.Data.Configurations;

public class CreditFacilityConfiguration : IEntityTypeConfiguration<CreditFacility>
{
    public void Configure(EntityTypeBuilder<CreditFacility> builder)
    {
        builder.Property(f => f.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(f => f.Balance)
            .HasPrecision(18, 2);

        builder.Property(f => f.AnnualInterestRate)
            .HasPrecision(9, 4);

        builder.Property(f => f.MonthlyPayment)
            .HasPrecision(18, 2);

        builder.HasOne(f => f.Person)
            .WithMany(p => p.CreditFacilities)
            .HasForeignKey(f => f.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
