using InsuranceApi.Application.Interfaces;
using InsuranceApi.Domain.Entities;
using InsuranceApi.Domain.Enums;
using InsuranceApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.Infrastructure.Repositories;

public class PolicyRepository : IPolicyRepository
{
    private readonly AppDbContext _db;

    public PolicyRepository(AppDbContext db) => _db = db;

    public Task<Policy?> GetByIdAsync(Guid id) =>
        _db.Policies.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IEnumerable<Policy>> GetAllAsync(PolicyType? type, PolicyStatus? status, Guid? customerId)
    {
        var query = _db.Policies.AsNoTracking().AsQueryable();
        if (type.HasValue) query = query.Where(p => p.Type == type.Value);
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (customerId.HasValue) query = query.Where(p => p.CustomerId == customerId.Value);
        return await query.ToListAsync();
    }

    public Task<bool> HasActiveOfTypeAsync(Guid customerId, PolicyType type) =>
        _db.Policies.AnyAsync(p =>
            p.CustomerId == customerId &&
            p.Type == type &&
            p.Status == PolicyStatus.Active);

    public async Task AddAsync(Policy policy)
    {
        _db.Policies.Add(policy);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Policy policy)
    {
        _db.Policies.Update(policy);
        await _db.SaveChangesAsync();
    }
}
