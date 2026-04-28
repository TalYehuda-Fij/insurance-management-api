using InsuranceApi.Application.DTOs.Customer;
using InsuranceApi.Application.Interfaces;
using InsuranceApi.Application.Services;
using InsuranceApi.Domain.Entities;
using InsuranceApi.Domain.Exceptions;
using NSubstitute;

namespace InsuranceApi.UnitTests.Services;

public class CustomerServiceTests
{
    private readonly ICustomerRepository _customerRepo = Substitute.For<ICustomerRepository>();
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        _sut = new CustomerService(_customerRepo);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCustomerResponse_WhenValid()
    {
        _customerRepo.IdNumberExistsAsync("ID123").Returns(false);

        var request = new CreateCustomerRequest("John", "Doe", "ID123",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
            "john@example.com", "0501234567");

        var result = await _sut.CreateAsync(request);

        Assert.Equal("John", result.FirstName);
        Assert.Equal("ID123", result.IdNumber);
        await _customerRepo.Received(1).AddAsync(Arg.Any<Customer>());
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenIdNumberAlreadyExists()
    {
        _customerRepo.IdNumberExistsAsync("DUPLICATE").Returns(true);

        var request = new CreateCustomerRequest("Jane", "Doe", "DUPLICATE",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-28)),
            null, "0509876543");

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenCustomerNotFound()
    {
        var id = Guid.NewGuid();
        _customerRepo.GetByIdAsync(id).Returns((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(id));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateFields_WhenCustomerExists()
    {
        var id = Guid.NewGuid();
        var existing = new Customer
        {
            Id = id,
            FirstName = "Old",
            LastName = "Name",
            IdNumber = "ID999",
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-40)),
            Phone = "0500000000",
            CreatedAt = DateTime.UtcNow
        };
        _customerRepo.GetByIdAsync(id).Returns(existing);

        var request = new UpdateCustomerRequest("New", "Name", "new@email.com", "0501111111");
        var result = await _sut.UpdateAsync(id, request);

        Assert.Equal("New", result.FirstName);
        Assert.Equal("new@email.com", result.Email);
        await _customerRepo.Received(1).UpdateAsync(Arg.Any<Customer>());
    }
}
