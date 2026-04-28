using InsuranceApi.Domain.Enums;

namespace InsuranceApi.Application.DTOs.Policy;

public record UpdatePolicyRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal PremiumAmount,
    PolicyType Type
);
