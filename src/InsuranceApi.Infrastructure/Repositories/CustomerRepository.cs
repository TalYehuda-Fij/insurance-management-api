using InsuranceApi.Application.Interfaces;
using InsuranceApi.Domain.Entities;
using InsuranceApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;

    public CustomerRepository(AppDbContext db) => _db = db;

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Customers.Include(c => c.Policies).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Customer?> GetByIdNumberAsync(string idNumber, CancellationToken cancellationToken = default) =>
        _db.Customers.Include(c => c.Policies).FirstOrDefaultAsync(c => c.IdNumber == idNumber, cancellationToken);

    public async Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Customers.AsNoTracking().ToListAsync(cancellationToken);

    public Task<bool> IdNumberExistsAsync(string idNumber, CancellationToken cancellationToken = default) =>
        _db.Customers.AnyAsync(c => c.IdNumber == idNumber, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, Guid? excludeCustomerId = null, CancellationToken cancellationToken = default) =>
        _db.Customers.AnyAsync(c =>
            c.Email == email &&
            (excludeCustomerId == null || c.Id != excludeCustomerId), cancellationToken);

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _db.Customers.Update(customer);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
