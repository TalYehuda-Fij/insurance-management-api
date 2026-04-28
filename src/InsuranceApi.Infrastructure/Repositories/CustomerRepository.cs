using InsuranceApi.Application.Interfaces;
using InsuranceApi.Domain.Entities;
using InsuranceApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;

    public CustomerRepository(AppDbContext db) => _db = db;

    public Task<Customer?> GetByIdAsync(Guid id) =>
        _db.Customers.Include(c => c.Policies).FirstOrDefaultAsync(c => c.Id == id);

    public Task<Customer?> GetByIdNumberAsync(string idNumber) =>
        _db.Customers.Include(c => c.Policies).FirstOrDefaultAsync(c => c.IdNumber == idNumber);

    public async Task<IEnumerable<Customer>> GetAllAsync() =>
        await _db.Customers.AsNoTracking().ToListAsync();

    public Task<bool> IdNumberExistsAsync(string idNumber) =>
        _db.Customers.AnyAsync(c => c.IdNumber == idNumber);

    public async Task AddAsync(Customer customer)
    {
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Customer customer)
    {
        _db.Customers.Update(customer);
        await _db.SaveChangesAsync();
    }
}
