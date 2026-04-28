using InsuranceApi.Domain.Entities;

namespace InsuranceApi.Application.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id);
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<bool> IdNumberExistsAsync(string idNumber);
    Task AddAsync(Customer customer);
    Task UpdateAsync(Customer customer);
}
