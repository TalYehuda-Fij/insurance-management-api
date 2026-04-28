using InsuranceApi.Domain.Enums;

namespace InsuranceApi.Application.DTOs.Policy;

public record CreatePolicyRequest(
    PolicyType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal PremiumAmount
);
