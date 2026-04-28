using InsuranceApi.Domain.Entities;
using InsuranceApi.Infrastructure.Repositories;
using InsuranceApi.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace InsuranceApi.IntegrationTests.Repositories;

public class CustomerRepositoryTests : IDisposable
{
    private readonly DbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static Customer NewCustomer(string idNumber = "ID-001", string? email = null) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Jane",
        LastName = "Doe",
        IdNumber = idNumber,
        DateOfBirth = new DateOnly(1990, 1, 1),
        Email = email,
        Phone = "0500000000",
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task AddAsync_ShouldPersistCustomer()
    {
        await using var ctx = _factory.CreateContext();
        var repo = new CustomerRepository(ctx);
        var customer = NewCustomer();

        await repo.AddAsync(customer);

        await using var verify = _factory.CreateContext();
        var saved = await verify.Customers.FindAsync(customer.Id);
        Assert.NotNull(saved);
        Assert.Equal("Jane", saved.FirstName);
        Assert.Equal("ID-001", saved.IdNumber);
    }

    [Fact]
    public async Task GetByIdNumberAsync_ShouldReturnCorrectCustomer()
    {
        await using var ctx = _factory.CreateContext();
        var repo = new CustomerRepository(ctx);
        await repo.AddAsync(NewCustomer("ID-ABC"));

        var result = await repo.GetByIdNumberAsync("ID-ABC");

        Assert.NotNull(result);
        Assert.Equal("ID-ABC", result.IdNumber);
    }

    [Fact]
    public async Task IdNumberExistsAsync_ShouldReturnTrue_WhenDuplicate()
    {
        await using var ctx = _factory.CreateContext();
        var repo = new CustomerRepository(ctx);
        await repo.AddAsync(NewCustomer("ID-DUP"));

        var exists = await repo.IdNumberExistsAsync("ID-DUP");

        Assert.True(exists);
    }

    [Fact]
    public async Task EmailExistsAsync_ShouldReturnTrue_WhenEmailTakenByAnotherCustomer()
    {
        await using var ctx = _factory.CreateContext();
        var repo = new CustomerRepository(ctx);
        var existing = NewCustomer("ID-E1", "taken@example.com");
        await repo.AddAsync(existing);
        var other = NewCustomer("ID-E2");

        var exists = await repo.EmailExistsAsync("taken@example.com", excludeCustomerId: other.Id);

        Assert.True(exists);
    }

    [Fact]
    public async Task EmailExistsAsync_ShouldReturnFalse_WhenEmailBelongsToSameCustomer()
    {
        await using var ctx = _factory.CreateContext();
        var repo = new CustomerRepository(ctx);
        var customer = NewCustomer("ID-E3", "self@example.com");
        await repo.AddAsync(customer);

        var exists = await repo.EmailExistsAsync("self@example.com", excludeCustomerId: customer.Id);

        Assert.False(exists);
    }

    [Fact]
    public async Task IdNumber_UniqueIndex_ShouldPreventDuplicates()
    {
        await using var ctx = _factory.CreateContext();
        ctx.Customers.Add(NewCustomer("ID-SAME"));
        ctx.Customers.Add(NewCustomer("ID-SAME"));

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Email_UniqueIndex_ShouldPreventDuplicates()
    {
        await using var ctx = _factory.CreateContext();
        ctx.Customers.Add(NewCustomer("ID-X1", "dup@example.com"));
        ctx.Customers.Add(NewCustomer("ID-X2", "dup@example.com"));

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        await using var ctx = _factory.CreateContext();
        var repo = new CustomerRepository(ctx);
        var customer = NewCustomer("ID-UPD");
        await repo.AddAsync(customer);

        await using var ctx2 = _factory.CreateContext();
        var repo2 = new CustomerRepository(ctx2);
        var loaded = await repo2.GetByIdNumberAsync("ID-UPD");
        loaded!.FirstName = "Updated";
        await repo2.UpdateAsync(loaded);

        await using var verify = _factory.CreateContext();
        var saved = await verify.Customers.FindAsync(customer.Id);
        Assert.Equal("Updated", saved!.FirstName);
    }
}
