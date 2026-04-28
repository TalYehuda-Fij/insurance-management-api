using InsuranceApi.Domain.Entities;

namespace InsuranceApi.Application.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer?> GetByIdNumberAsync(string idNumber);
    Task<IEnumerable<Customer>> GetAllAsync();
    Task<bool> IdNumberExistsAsync(string idNumber);
    Task<bool> EmailExistsAsync(string email, Guid? excludeCustomerId = null);
    Task AddAsync(Customer customer);
    Task UpdateAsync(Customer customer);
}
