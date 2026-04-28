using InsuranceApi.Application.DTOs.Policy;
using InsuranceApi.Application.Interfaces;
using InsuranceApi.Domain.Entities;
using InsuranceApi.Domain.Enums;
using InsuranceApi.Domain.Exceptions;
using InsuranceApi.Domain.Rules;

namespace InsuranceApi.Application.Services;

public class PolicyService
{
    private readonly IPolicyRepository _policyRepository;
    private readonly ICustomerRepository _customerRepository;

    public PolicyService(IPolicyRepository policyRepository, ICustomerRepository customerRepository)
    {
        _policyRepository = policyRepository;
        _customerRepository = customerRepository;
    }

    public async Task<PolicyResponse> CreateAsync(string idNumber, CreatePolicyRequest request)
    {
        var customer = await _customerRepository.GetByIdNumberAsync(idNumber)
            ?? throw new NotFoundException($"Customer with ID number '{idNumber}' not found.");

        if (request.EndDate < request.StartDate)
            throw new BusinessRuleException("Policy end date cannot be before start date.");

        PolicyTypeRules.ValidatePremium(request.Type, request.PremiumAmount);
        PolicyTypeRules.ValidateDuration(request.Type, request.StartDate, request.EndDate);

        if (await _policyRepository.HasActiveOfTypeAsync(customer.Id, request.Type))
            throw new BusinessRuleException($"Customer already has an active {request.Type} policy.");

        var policy = new Policy
        {
            Id = Guid.NewGuid(),
            PolicyNumber = GeneratePolicyNumber(),
            Type = request.Type,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PremiumAmount = request.PremiumAmount,
            Status = PolicyStatus.Active,
            CustomerId = customer.Id,
            CreatedAt = DateTime.UtcNow
        };

        await _policyRepository.AddAsync(policy);
        return ToResponse(policy);
    }

    public async Task<IEnumerable<PolicyResponse>> GetAllAsync(PolicyType? type, PolicyStatus? status, string? idNumber)
    {
        Guid? customerId = null;
        if (idNumber is not null)
        {
            var customer = await _customerRepository.GetByIdNumberAsync(idNumber)
                ?? throw new NotFoundException($"Customer with ID number '{idNumber}' not found.");
            customerId = customer.Id;
        }

        var policies = await _policyRepository.GetAllAsync(type, status, customerId);
        return policies.Select(ToResponse);
    }

    public async Task<PolicyResponse> GetByIdAsync(Guid id)
    {
        var policy = await _policyRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Policy {id} not found.");
        return ToResponse(policy);
    }

    public async Task<PolicyResponse> UpdateAsync(Guid id, UpdatePolicyRequest request)
    {
        var policy = await _policyRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Policy {id} not found.");

        if (request.EndDate < request.StartDate)
            throw new BusinessRuleException("Policy end date cannot be before start date.");

        PolicyTypeRules.ValidatePremium(request.Type, request.PremiumAmount);
        PolicyTypeRules.ValidateDuration(request.Type, request.StartDate, request.EndDate);

        if (policy.Status == PolicyStatus.Active &&
            policy.Type != request.Type &&
            await _policyRepository.HasActiveOfTypeAsync(policy.CustomerId, request.Type))
        {
            throw new BusinessRuleException($"Customer already has an active {request.Type} policy.");
        }

        policy.StartDate = request.StartDate;
        policy.EndDate = request.EndDate;
        policy.PremiumAmount = request.PremiumAmount;
        policy.Type = request.Type;

        await _policyRepository.UpdateAsync(policy);
        return ToResponse(policy);
    }

    public async Task<PolicyResponse> CancelAsync(Guid id)
    {
        var policy = await _policyRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Policy {id} not found.");

        if (policy.Status == PolicyStatus.Cancelled)
            throw new BusinessRuleException("Policy is already cancelled.");

        policy.Status = PolicyStatus.Cancelled;
        await _policyRepository.UpdateAsync(policy);
        return ToResponse(policy);
    }

    private static string GeneratePolicyNumber() =>
        $"POL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

    private static PolicyResponse ToResponse(Policy p) =>
        new(p.Id, p.PolicyNumber, p.Type, p.StartDate, p.EndDate, p.PremiumAmount, p.Status, p.CustomerId, p.CreatedAt);
}
