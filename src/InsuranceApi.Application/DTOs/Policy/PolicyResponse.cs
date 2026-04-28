using InsuranceApi.Domain.Enums;

namespace InsuranceApi.Application.DTOs.Policy;

public record PolicyResponse(
    Guid Id,
    string PolicyNumber,
    PolicyType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal PremiumAmount,
    PolicyStatus Status,
    Guid CustomerId,
    DateTime CreatedAt
);
