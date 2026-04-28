namespace InsuranceApi.Application.DTOs.Customer;

public record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string IdNumber,
    DateOnly DateOfBirth,
    string? Email,
    string Phone
);
