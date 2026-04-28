using InsuranceApi.Domain.Enums;
using InsuranceApi.Domain.Exceptions;

namespace InsuranceApi.Domain.Rules;

public static class PolicyTypeRules
{
    private static readonly Dictionary<PolicyType, (decimal Min, decimal Max)> PremiumRanges = new()
    {
        [PolicyType.Car] = (500m, 50_000m),
        [PolicyType.Health] = (200m, 30_000m),
        [PolicyType.Life] = (100m, 100_000m),
        [PolicyType.Home] = (1_000m, 50_000m)
    };

    private static readonly Dictionary<PolicyType, (int MinDays, int MaxDays)> DurationRanges = new()
    {
        [PolicyType.Car] = (180, 731),
        [PolicyType.Health] = (180, 731),
        [PolicyType.Life] = (3650, 10958),
        [PolicyType.Home] = (180, 731)
    };

    public static void ValidatePremium(PolicyType type, decimal premium)
    {
        var (min, max) = PremiumRanges[type];
        if (premium < min || premium > max)
            throw new BusinessRuleException(
                $"Premium for {type} policies must be between {min:N0} and {max:N0}.");
    }

    public static void ValidateDuration(PolicyType type, DateOnly startDate, DateOnly endDate)
    {
        var (minDays, maxDays) = DurationRanges[type];
        var days = endDate.DayNumber - startDate.DayNumber;
        if (days < minDays || days > maxDays)
            throw new BusinessRuleException(
                $"{type} policy duration must be between {minDays} and {maxDays} days.");
    }
}
