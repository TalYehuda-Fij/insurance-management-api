using InsuranceApi.Domain.Entities;
using InsuranceApi.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Policy> Policies => Set<Policy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new PolicyConfiguration());
    }
}
