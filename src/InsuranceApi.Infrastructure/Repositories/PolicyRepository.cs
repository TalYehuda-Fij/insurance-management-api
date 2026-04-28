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

    public Task<Policy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Policies.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IEnumerable<Policy>> GetAllAsync(PolicyType? type, PolicyStatus? status, Guid? customerId, CancellationToken cancellationToken = default)
    {
        var query = _db.Policies.AsNoTracking().AsQueryable();
        if (type.HasValue) query = query.Where(p => p.Type == type.Value);
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (customerId.HasValue) query = query.Where(p => p.CustomerId == customerId.Value);
        return await query.ToListAsync(cancellationToken);
    }

    public Task<bool> HasActiveOfTypeAsync(Guid customerId, PolicyType type, CancellationToken cancellationToken = default) =>
        _db.Policies.AnyAsync(p =>
            p.CustomerId == customerId &&
            p.Type == type &&
            p.Status == PolicyStatus.Active, cancellationToken);

    public async Task AddAsync(Policy policy, CancellationToken cancellationToken = default)
    {
        _db.Policies.Add(policy);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Policy policy, CancellationToken cancellationToken = default)
    {
        _db.Policies.Update(policy);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
