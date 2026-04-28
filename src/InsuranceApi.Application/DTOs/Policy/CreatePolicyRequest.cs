using System.ComponentModel.DataAnnotations;
using InsuranceApi.Domain.Enums;

namespace InsuranceApi.Application.DTOs.Policy;

public record CreatePolicyRequest(
    [Required, EnumDataType(typeof(PolicyType))] PolicyType Type,
    [Required] DateOnly StartDate,
    [Required] DateOnly EndDate,
    [Range(0.01, double.MaxValue)] decimal PremiumAmount
);
