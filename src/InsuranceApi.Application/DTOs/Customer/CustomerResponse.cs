namespace InsuranceApi.Application.DTOs.Customer;

public record CustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string IdNumber,
    DateOnly DateOfBirth,
    string? Email,
    string Phone,
    DateTime CreatedAt
);
