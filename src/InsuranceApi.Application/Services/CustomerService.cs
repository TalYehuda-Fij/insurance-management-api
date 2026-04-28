using InsuranceApi.Application.DTOs.Customer;
using InsuranceApi.Application.Interfaces;
using InsuranceApi.Domain.Entities;
using InsuranceApi.Domain.Exceptions;

namespace InsuranceApi.Application.Services;

public class CustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request)
    {
        if (await _customerRepository.IdNumberExistsAsync(request.IdNumber))
            throw new BusinessRuleException($"A customer with ID number '{request.IdNumber}' already exists.");

        var normalizedEmail = NormalizeEmail(request.Email);
        if (normalizedEmail is not null && await _customerRepository.EmailExistsAsync(normalizedEmail))
            throw new BusinessRuleException($"A customer with email '{normalizedEmail}' already exists.");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            IdNumber = request.IdNumber,
            DateOfBirth = request.DateOfBirth,
            Email = normalizedEmail,
            Phone = request.Phone,
            CreatedAt = DateTime.UtcNow
        };

        await _customerRepository.AddAsync(customer);
        return ToResponse(customer);
    }

    public async Task<IEnumerable<CustomerResponse>> GetAllAsync()
    {
        var customers = await _customerRepository.GetAllAsync();
        return customers.Select(ToResponse);
    }

    public async Task<CustomerResponse> GetByIdNumberAsync(string idNumber)
    {
        var customer = await _customerRepository.GetByIdNumberAsync(idNumber)
            ?? throw new NotFoundException($"Customer with ID number '{idNumber}' not found.");
        return ToResponse(customer);
    }

    public async Task<CustomerResponse> UpdateAsync(string idNumber, UpdateCustomerRequest request)
    {
        var customer = await _customerRepository.GetByIdNumberAsync(idNumber)
            ?? throw new NotFoundException($"Customer with ID number '{idNumber}' not found.");

        var normalizedEmail = NormalizeEmail(request.Email);
        if (normalizedEmail is not null &&
            await _customerRepository.EmailExistsAsync(normalizedEmail, excludeCustomerId: customer.Id))
        {
            throw new BusinessRuleException($"A customer with email '{normalizedEmail}' already exists.");
        }

        customer.FirstName = request.FirstName;
        customer.LastName = request.LastName;
        customer.Email = normalizedEmail;
        customer.Phone = request.Phone;

        await _customerRepository.UpdateAsync(customer);
        return ToResponse(customer);
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        return email.Trim().ToLowerInvariant();
    }

    private static CustomerResponse ToResponse(Customer c) =>
        new(c.Id, c.FirstName, c.LastName, c.IdNumber, c.DateOfBirth, c.Email, c.Phone, c.CreatedAt);
}
