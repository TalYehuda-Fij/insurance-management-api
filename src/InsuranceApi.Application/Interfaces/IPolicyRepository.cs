using InsuranceApi.Domain.Entities;
using InsuranceApi.Domain.Enums;

namespace InsuranceApi.Application.Interfaces;

public interface IPolicyRepository
{
    Task<Policy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Policy>> GetAllAsync(PolicyType? type, PolicyStatus? status, Guid? customerId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveOfTypeAsync(Guid customerId, PolicyType type, CancellationToken cancellationToken = default);
    Task AddAsync(Policy policy, CancellationToken cancellationToken = default);
    Task UpdateAsync(Policy policy, CancellationToken cancellationToken = default);
}
