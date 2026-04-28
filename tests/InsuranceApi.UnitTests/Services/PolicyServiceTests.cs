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

    private static CreatePolicyRequest FutureCarPolicy() => new(
        PolicyType.Car,
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
        DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
        1500m
    );

    // --- Rule 1: No duplicate active policy of same type ---

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenCustomerAlreadyHasActivePolicyOfSameType()
    {
        var customer = AdultCustomer();
        _customerRepo.GetByIdNumberAsync(AdultIdNumber).Returns(customer);
        _policyRepo.HasActiveOfTypeAsync(customer.Id, PolicyType.Car).Returns(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(AdultIdNumber, FutureCarPolicy()));
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenCustomerHasNoActivePolicyOfSameType()
    {
        var customer = AdultCustomer();
        _customerRepo.GetByIdNumberAsync(AdultIdNumber).Returns(customer);
        _policyRepo.HasActiveOfTypeAsync(customer.Id, PolicyType.Car).Returns(false);

        var result = await _sut.CreateAsync(AdultIdNumber, FutureCarPolicy());

        Assert.Equal(PolicyType.Car, result.Type);
        Assert.Equal(PolicyStatus.Active, result.Status);
    }

    // --- Rule 2: Start date not in past ---

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenStartDateIsInThePast()
    {
        _customerRepo.GetByIdNumberAsync(AdultIdNumber).Returns(AdultCustomer());
        _policyRepo.HasActiveOfTypeAsync(Arg.Any<Guid>(), Arg.Any<PolicyType>()).Returns(false);

        var pastRequest = new CreatePolicyRequest(
            PolicyType.Health,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            500m
        );

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(AdultIdNumber, pastRequest));
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenStartDateIsToday()
    {
        _customerRepo.GetByIdNumberAsync(AdultIdNumber).Returns(AdultCustomer());
        _policyRepo.HasActiveOfTypeAsync(Arg.Any<Guid>(), Arg.Any<PolicyType>()).Returns(false);

        var todayRequest = new CreatePolicyRequest(
            PolicyType.Life,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            800m
        );

        var result = await _sut.CreateAsync(AdultIdNumber, todayRequest);

        Assert.Equal(PolicyStatus.Active, result.Status);
    }

    // --- EndDate must be on or after StartDate ---

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenEndDateIsBeforeStartDate()
    {
        _customerRepo.GetByIdNumberAsync(AdultIdNumber).Returns(AdultCustomer());
        _policyRepo.HasActiveOfTypeAsync(Arg.Any<Guid>(), Arg.Any<PolicyType>()).Returns(false);

        var invalidRequest = new CreatePolicyRequest(
            PolicyType.Home,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            1000m
        );

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(AdultIdNumber, invalidRequest));
    }

    // --- Rule 3: Customer must be 18+ ---

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenCustomerIsUnder18()
    {
        const string minorIdNumber = "ID-MINOR-999";
        var minorCustomer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Young",
            LastName = "One",
            IdNumber = minorIdNumber,
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17)),
            Phone = "0500000001",
            CreatedAt = DateTime.UtcNow
        };

        _customerRepo.GetByIdNumberAsync(minorIdNumber).Returns(minorCustomer);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(minorIdNumber, FutureCarPolicy()));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenCustomerNotFound()
    {
        _customerRepo.GetByIdNumberAsync("UNKNOWN").Returns((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.CreateAsync("UNKNOWN", FutureCarPolicy()));
    }

    // --- Update: re-check duplicate-active rule when type changes ---

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
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            PremiumAmount = 500m,
            Status = PolicyStatus.Active,
            CustomerId = customerId,
            CreatedAt = DateTime.UtcNow
        };
        _policyRepo.GetByIdAsync(policyId).Returns(existing);
        _policyRepo.HasActiveOfTypeAsync(customerId, PolicyType.Car).Returns(true);

        var request = new UpdatePolicyRequest(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            500m,
            PolicyType.Car
        );

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(policyId, request));
    }

    // --- Cancel ---

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
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
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
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            PremiumAmount = 1200m,
            Status = PolicyStatus.Cancelled,
            CustomerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        _policyRepo.GetByIdAsync(policyId).Returns(policy);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CancelAsync(policyId));
    }
}
