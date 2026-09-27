using Budge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budge.Infrastructure.Data.Configurations;

public class LedgerSettingsConfiguration : IEntityTypeConfiguration<LedgerSettings>
{
    public void Configure(EntityTypeBuilder<LedgerSettings> builder)
    {
        builder.Property(s => s.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();
    }
}
