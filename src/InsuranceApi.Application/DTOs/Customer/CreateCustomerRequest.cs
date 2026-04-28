using System.ComponentModel.DataAnnotations;

namespace InsuranceApi.Application.DTOs.Customer;

public record CreateCustomerRequest(
    [Required, StringLength(100, MinimumLength = 1)] string FirstName,
    [Required, StringLength(100, MinimumLength = 1)] string LastName,
    [Required, StringLength(50, MinimumLength = 1)] string IdNumber,
    [Required] DateOnly DateOfBirth,
    [EmailAddress, StringLength(200)] string? Email,
    [Required, StringLength(20, MinimumLength = 1)] string Phone
);
