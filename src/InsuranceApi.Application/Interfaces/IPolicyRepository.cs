using InsuranceApi.Domain.Entities;
using InsuranceApi.Domain.Enums;

namespace InsuranceApi.Application.Interfaces;

public interface IPolicyRepository
{
    Task<Policy?> GetByIdAsync(Guid id);
    Task<IEnumerable<Policy>> GetAllAsync(PolicyType? type, PolicyStatus? status, Guid? customerId);
    Task<bool> HasActiveOfTypeAsync(Guid customerId, PolicyType type);
    Task AddAsync(Policy policy);
    Task UpdateAsync(Policy policy);
}
