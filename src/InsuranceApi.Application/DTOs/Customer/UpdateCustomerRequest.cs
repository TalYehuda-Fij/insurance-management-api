using System.ComponentModel.DataAnnotations;

namespace InsuranceApi.Application.DTOs.Customer;

public record UpdateCustomerRequest(
    [Required, StringLength(100, MinimumLength = 1)] string FirstName,
    [Required, StringLength(100, MinimumLength = 1)] string LastName,
    [EmailAddress, StringLength(200)] string? Email,
    [Required, StringLength(20, MinimumLength = 1)] string Phone
);
