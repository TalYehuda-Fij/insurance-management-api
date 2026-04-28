using InsuranceApi.Domain.Entities;
using InsuranceApi.Domain.Enums;
using InsuranceApi.Infrastructure.Repositories;
using InsuranceApi.IntegrationTests.Helpers;

namespace InsuranceApi.IntegrationTests.Repositories;

public class PolicyRepositoryTests : IDisposable
{
    private readonly DbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static Customer NewCustomer(string idNumber = "ID-001") => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Jane",
        LastName = "Doe",
        IdNumber = idNumber,
        DateOfBirth = new DateOnly(1990, 1, 1),
        Phone = "0500000000",
        CreatedAt = DateTime.UtcNow
    };

    private static Policy NewPolicy(Guid customerId, PolicyType type = PolicyType.Car, PolicyStatus status = PolicyStatus.Active) => new()
    {
        Id = Guid.NewGuid(),
        PolicyNumber = $"POL-{Guid.NewGuid():N}",
        Type = type,
        StartDate = new DateOnly(2026, 5, 1),
        EndDate = new DateOnly(2027, 5, 1),
        PremiumAmount = 1500m,
        Status = status,
        CustomerId = customerId,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task AddAsync_ShouldPersistPolicy()
    {
        await using var ctx = _factory.CreateContext();
        var customerRepo = new CustomerRepository(ctx);
        var customer = NewCustomer();
        await customerRepo.AddAsync(customer);

        await using var ctx2 = _factory.CreateContext();
        var policyRepo = new PolicyRepository(ctx2);
        var policy = NewPolicy(customer.Id);
        await policyRepo.AddAsync(policy);

        await using var verify = _factory.CreateContext();
        var saved = await verify.Policies.FindAsync(policy.Id);
        Assert.NotNull(saved);
        Assert.Equal(PolicyType.Car, saved.Type);
        Assert.Equal(PolicyStatus.Active, saved.Status);
        Assert.Equal(1500m, saved.PremiumAmount);
    }

    [Fact]
    public async Task HasActiveOfTypeAsync_ShouldReturnTrue_WhenActiveExists()
    {
        await using var ctx = _factory.CreateContext();
        var customerRepo = new CustomerRepository(ctx);
        var customer = NewCustomer("ID-P1");
        await customerRepo.AddAsync(customer);

        await using var ctx2 = _factory.CreateContext();
        var policyRepo = new PolicyRepository(ctx2);
        await policyRepo.AddAsync(NewPolicy(customer.Id, PolicyType.Health));

        await using var ctx3 = _factory.CreateContext();
        var repo3 = new PolicyRepository(ctx3);
        var result = await repo3.HasActiveOfTypeAsync(customer.Id, PolicyType.Health);

        Assert.True(result);
    }

    [Fact]
    public async Task HasActiveOfTypeAsync_ShouldReturnFalse_WhenPolicyCancelled()
    {
        await using var ctx = _factory.CreateContext();
        var customerRepo = new CustomerRepository(ctx);
        var customer = NewCustomer("ID-P2");
        await customerRepo.AddAsync(customer);

        await using var ctx2 = _factory.CreateContext();
        var policyRepo = new PolicyRepository(ctx2);
        await policyRepo.AddAsync(NewPolicy(customer.Id, PolicyType.Life, PolicyStatus.Cancelled));

        await using var ctx3 = _factory.CreateContext();
        var repo3 = new PolicyRepository(ctx3);
        var result = await repo3.HasActiveOfTypeAsync(customer.Id, PolicyType.Life);

        Assert.False(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByTypeAndStatus()
    {
        await using var ctx = _factory.CreateContext();
        var customerRepo = new CustomerRepository(ctx);
        var customer = NewCustomer("ID-P3");
        await customerRepo.AddAsync(customer);

        await using var ctx2 = _factory.CreateContext();
        var policyRepo = new PolicyRepository(ctx2);
        await policyRepo.AddAsync(NewPolicy(customer.Id, PolicyType.Car, PolicyStatus.Active));
        await policyRepo.AddAsync(NewPolicy(customer.Id, PolicyType.Home, PolicyStatus.Cancelled));

        await using var ctx3 = _factory.CreateContext();
        var repo3 = new PolicyRepository(ctx3);
        var results = await repo3.GetAllAsync(PolicyType.Car, PolicyStatus.Active, null);

        Assert.Single(results);
        Assert.All(results, p => Assert.Equal(PolicyType.Car, p.Type));
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByCustomerId()
    {
        await using var ctx = _factory.CreateContext();
        var customerRepo = new CustomerRepository(ctx);
        var c1 = NewCustomer("ID-P4");
        var c2 = NewCustomer("ID-P5");
        await customerRepo.AddAsync(c1);
        await customerRepo.AddAsync(c2);

        await using var ctx2 = _factory.CreateContext();
        var policyRepo = new PolicyRepository(ctx2);
        await policyRepo.AddAsync(NewPolicy(c1.Id, PolicyType.Car));
        await policyRepo.AddAsync(NewPolicy(c2.Id, PolicyType.Home));

        await using var ctx3 = _factory.CreateContext();
        var repo3 = new PolicyRepository(ctx3);
        var results = await repo3.GetAllAsync(null, null, c1.Id);

        Assert.Single(results);
        Assert.All(results, p => Assert.Equal(c1.Id, p.CustomerId));
    }

    [Fact]
    public async Task CancelAsync_ShouldPersistCancelledStatus()
    {
        await using var ctx = _factory.CreateContext();
        var customerRepo = new CustomerRepository(ctx);
        var customer = NewCustomer("ID-P6");
        await customerRepo.AddAsync(customer);

        await using var ctx2 = _factory.CreateContext();
        var policyRepo = new PolicyRepository(ctx2);
        var policy = NewPolicy(customer.Id);
        await policyRepo.AddAsync(policy);

        await using var ctx3 = _factory.CreateContext();
        var repo3 = new PolicyRepository(ctx3);
        var loaded = await repo3.GetByIdAsync(policy.Id);
        loaded!.Status = PolicyStatus.Cancelled;
        await repo3.UpdateAsync(loaded);

        await using var verify = _factory.CreateContext();
        var saved = await verify.Policies.FindAsync(policy.Id);
        Assert.Equal(PolicyStatus.Cancelled, saved!.Status);
    }
}
