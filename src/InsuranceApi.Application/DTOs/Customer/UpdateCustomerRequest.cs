namespace InsuranceApi.Application.DTOs.Customer;

public record UpdateCustomerRequest(
    string FirstName,
    string LastName,
    string? Email,
    string Phone
);
