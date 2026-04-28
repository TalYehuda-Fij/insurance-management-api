using InsuranceApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsuranceApi.Infrastructure.Persistence.Configurations;

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PolicyNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(p => p.PolicyNumber).IsUnique();
        builder.Property(p => p.PremiumAmount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Type).HasConversion<string>();
        builder.Property(p => p.Status).HasConversion<string>();
    }
}
