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

    public async Task<PolicyResponse> CreateAsync(string idNumber, CreatePolicyRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdNumberAsync(idNumber, cancellationToken)
            ?? throw new NotFoundException($"Customer with ID number '{idNumber}' not found.");

        if (request.EndDate < request.StartDate)
            throw new BusinessRuleException("Policy end date cannot be before start date.");

        PolicyTypeRules.ValidatePremium(request.Type, request.PremiumAmount);
        PolicyTypeRules.ValidateDuration(request.Type, request.StartDate, request.EndDate);

        if (await _policyRepository.HasActiveOfTypeAsync(customer.Id, request.Type, cancellationToken))
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

        await _policyRepository.AddAsync(policy, cancellationToken);
        return ToResponse(policy);
    }

    public async Task<IEnumerable<PolicyResponse>> GetAllAsync(PolicyType? type, PolicyStatus? status, string? idNumber, CancellationToken cancellationToken = default)
    {
        Guid? customerId = null;
        if (idNumber is not null)
        {
            var customer = await _customerRepository.GetByIdNumberAsync(idNumber, cancellationToken)
                ?? throw new NotFoundException($"Customer with ID number '{idNumber}' not found.");
            customerId = customer.Id;
        }

        var policies = await _policyRepository.GetAllAsync(type, status, customerId, cancellationToken);
        return policies.Select(ToResponse);
    }

    public async Task<PolicyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await _policyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Policy {id} not found.");
        return ToResponse(policy);
    }

    public async Task<PolicyResponse> UpdateAsync(Guid id, UpdatePolicyRequest request, CancellationToken cancellationToken = default)
    {
        var policy = await _policyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Policy {id} not found.");

        if (request.EndDate < request.StartDate)
            throw new BusinessRuleException("Policy end date cannot be before start date.");

        PolicyTypeRules.ValidatePremium(request.Type, request.PremiumAmount);
        PolicyTypeRules.ValidateDuration(request.Type, request.StartDate, request.EndDate);

        if (policy.Status == PolicyStatus.Active &&
            policy.Type != request.Type &&
            await _policyRepository.HasActiveOfTypeAsync(policy.CustomerId, request.Type, cancellationToken))
        {
            throw new BusinessRuleException($"Customer already has an active {request.Type} policy.");
        }

        policy.StartDate = request.StartDate;
        policy.EndDate = request.EndDate;
        policy.PremiumAmount = request.PremiumAmount;
        policy.Type = request.Type;

        await _policyRepository.UpdateAsync(policy, cancellationToken);
        return ToResponse(policy);
    }

    public async Task<PolicyResponse> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await _policyRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Policy {id} not found.");

        if (policy.Status == PolicyStatus.Cancelled)
            throw new BusinessRuleException("Policy is already cancelled.");

        policy.Status = PolicyStatus.Cancelled;
        await _policyRepository.UpdateAsync(policy, cancellationToken);
        return ToResponse(policy);
    }

    private static string GeneratePolicyNumber() =>
        $"POL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

    private static PolicyResponse ToResponse(Policy p) =>
        new(p.Id, p.PolicyNumber, p.Type, p.StartDate, p.EndDate, p.PremiumAmount, p.Status, p.CustomerId, p.CreatedAt);
}
