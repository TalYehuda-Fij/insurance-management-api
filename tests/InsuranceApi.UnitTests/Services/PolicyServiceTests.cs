using InsuranceApi.Application.DTOs.Policy;
using InsuranceApi.Application.Interfaces;
using InsuranceApi.Application.Services;
using InsuranceApi.Domain.Entities;
using InsuranceApi.Domain.Enums;
using InsuranceApi.Domain.Exceptions;
using NSubstitute;

namespace InsuranceApi.UnitTests.Services;

public class PolicyServiceTests
{
    private readonly IPolicyRepository _policyRepo = Substitute.For<IPolicyRepository>();
    private readonly ICustomerRepository _customerRepo = Substitute.For<ICustomerRepository>();
    private readonly PolicyService _sut;

    public PolicyServiceTests()
    {
        _sut = new PolicyService(_policyRepo, _customerRepo);
    }

    private const string AdultIdNumber = "ID-ADULT-123";

    private static Customer AdultCustomer() => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Jane",
        LastName = "Doe",
        IdNumber = AdultIdNumber,
        DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
        Phone = "0500000000",
        CreatedAt = DateTime.UtcNow
    };

    private static CreatePolicyRequest ValidCarPolicy() => new(
        PolicyType.Car,
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(366)),
        1500m
    );

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenCustomerAlreadyHasActivePolicyOfSameType()
    {
        var customer = AdultCustomer();
        _customerRepo.GetByIdNumberAsync(AdultIdNumber).Returns(customer);
        _policyRepo.HasActiveOfTypeAsync(customer.Id, PolicyType.Car).Returns(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(AdultIdNumber, ValidCarPolicy()));
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenCustomerHasNoActivePolicyOfSameType()
    {
        var customer = AdultCustomer();
        _customerRepo.GetByIdNumberAsync(AdultIdNumber).Returns(customer);
        _policyRepo.HasActiveOfTypeAsync(customer.Id, PolicyType.Car).Returns(false);

        var result = await _sut.CreateAsync(AdultIdNumber, ValidCarPolicy());

        Assert.Equal(PolicyType.Car, result.Type);
        Assert.Equal(PolicyStatus.Active, result.Status);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenPremiumIsBelowTypeRange()
    {
        _customerRepo.GetByIdNumberAsync(AdultIdNumber).Returns(AdultCustomer());
        _policyRepo.HasActiveOfTypeAsync(Arg.Any<Guid>(), Arg.Any<PolicyType>()).Returns(false);

        var underpriced = new CreatePolicyRequest(
            PolicyType.Car,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(366)),
            100m
        );

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(AdultIdNumber, underpriced));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenPremiumIsAboveTypeRange()
    {
        _customerRepo.GetByIdNumberAsync(AdultIdNumber).Returns(AdultCustomer());
        _policyRepo.HasActiveOfTypeAsync(Arg.Any<Guid>(), Arg.Any<PolicyType>()).Returns(false);

        var overpriced = new CreatePolicyRequest(
            PolicyType.Health,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(366)),
            999_999m
        );

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(AdultIdNumber, overpriced));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenDurationIsTooShortForType()
    {
        _customerRepo.GetByIdNumberAsync(AdultIdNumber).Returns(AdultCustomer());
        _policyRepo.HasActiveOfTypeAsync(Arg.Any<Guid>(), Arg.Any<PolicyType>()).Returns(false);

        var tooShort = new CreatePolicyRequest(
            PolicyType.Car,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            1500m
        );

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(AdultIdNumber, tooShort));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenDurationIsTooLongForType()
    {
        _customerRepo.GetByIdNumberAsync(AdultIdNumber).Returns(AdultCustomer());
        _policyRepo.HasActiveOfTypeAsync(Arg.Any<Guid>(), Arg.Any<PolicyType>()).Returns(false);

        var tooLong = new CreatePolicyRequest(
            PolicyType.Home,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2000)),
            5000m
        );

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(AdultIdNumber, tooLong));
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenLifePolicyHasLongDuration()
    {
        _customerRepo.GetByIdNumberAsync(AdultIdNumber).Returns(AdultCustomer());
        _policyRepo.HasActiveOfTypeAsync(Arg.Any<Guid>(), Arg.Any<PolicyType>()).Returns(false);

        var lifePolicy = new CreatePolicyRequest(
            PolicyType.Life,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(365 * 20 + 1)),
            5000m
        );

        var result = await _sut.CreateAsync(AdultIdNumber, lifePolicy);

        Assert.Equal(PolicyType.Life, result.Type);
        Assert.Equal(PolicyStatus.Active, result.Status);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenEndDateIsBeforeStartDate()
    {
        _customerRepo.GetByIdNumberAsync(AdultIdNumber).Returns(AdultCustomer());
        _policyRepo.HasActiveOfTypeAsync(Arg.Any<Guid>(), Arg.Any<PolicyType>()).Returns(false);

        var inverted = new CreatePolicyRequest(
            PolicyType.Home,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            5000m
        );

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(AdultIdNumber, inverted));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenCustomerNotFound()
    {
        _customerRepo.GetByIdNumberAsync("UNKNOWN").Returns((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.CreateAsync("UNKNOWN", ValidCarPolicy()));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenChangingTypeWouldCreateDuplicateActive()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var existing = new Policy
        {
            Id = policyId,
            PolicyNumber = "POL-A",
            Type = PolicyType.Health,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(366)),
            PremiumAmount = 500m,
            Status = PolicyStatus.Active,
            CustomerId = customerId,
            CreatedAt = DateTime.UtcNow
        };
        _policyRepo.GetByIdAsync(policyId).Returns(existing);
        _policyRepo.HasActiveOfTypeAsync(customerId, PolicyType.Car).Returns(true);

        var request = new UpdatePolicyRequest(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(366)),
            1500m,
            PolicyType.Car
        );

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(policyId, request));
    }

    [Fact]
    public async Task CancelAsync_ShouldSetStatusToCancelled_WhenPolicyIsActive()
    {
        var policyId = Guid.NewGuid();
        var policy = new Policy
        {
            Id = policyId,
            PolicyNumber = "POL-001",
            Type = PolicyType.Home,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(366)),
            PremiumAmount = 1200m,
            Status = PolicyStatus.Active,
            CustomerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        _policyRepo.GetByIdAsync(policyId).Returns(policy);

        var result = await _sut.CancelAsync(policyId);

        Assert.Equal(PolicyStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task CancelAsync_ShouldThrow_WhenPolicyAlreadyCancelled()
    {
        var policyId = Guid.NewGuid();
        var policy = new Policy
        {
            Id = policyId,
            PolicyNumber = "POL-002",
            Type = PolicyType.Home,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(366)),
            PremiumAmount = 1200m,
            Status = PolicyStatus.Cancelled,
            CustomerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        _policyRepo.GetByIdAsync(policyId).Returns(policy);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CancelAsync(policyId));
    }
}
