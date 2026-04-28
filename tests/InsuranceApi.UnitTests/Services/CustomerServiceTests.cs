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
    public async Task CreateAsync_ShouldThrow_WhenEmailAlreadyExists()
    {
        _customerRepo.IdNumberExistsAsync("ID-NEW").Returns(false);
        _customerRepo.EmailExistsAsync("dup@example.com", null).Returns(true);

        var request = new CreateCustomerRequest("Sam", "Smith", "ID-NEW",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
            "Dup@Example.com", "0501112222");

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeEmailToLowercase()
    {
        _customerRepo.IdNumberExistsAsync(Arg.Any<string>()).Returns(false);
        _customerRepo.EmailExistsAsync(Arg.Any<string>(), null).Returns(false);

        var request = new CreateCustomerRequest("Sam", "Smith", "ID-CASE",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
            "  Mixed.Case@Example.COM  ", "0501112222");

        var result = await _sut.CreateAsync(request);

        Assert.Equal("mixed.case@example.com", result.Email);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenEmailBelongsToAnotherCustomer()
    {
        const string idNumber = "ID-555";
        var existing = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Sam",
            LastName = "Smith",
            IdNumber = idNumber,
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
            Phone = "0501112222",
            CreatedAt = DateTime.UtcNow
        };
        _customerRepo.GetByIdNumberAsync(idNumber).Returns(existing);
        _customerRepo.EmailExistsAsync("taken@example.com", existing.Id).Returns(true);

        var request = new UpdateCustomerRequest("Sam", "Smith", "taken@example.com", "0501112222");

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(idNumber, request));
    }

    [Fact]
    public async Task UpdateAsync_ShouldSucceed_WhenKeepingOwnEmail()
    {
        const string idNumber = "ID-666";
        var existing = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Sam",
            LastName = "Smith",
            IdNumber = idNumber,
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
            Email = "self@example.com",
            Phone = "0501112222",
            CreatedAt = DateTime.UtcNow
        };
        _customerRepo.GetByIdNumberAsync(idNumber).Returns(existing);
        _customerRepo.EmailExistsAsync("self@example.com", existing.Id).Returns(false);

        var request = new UpdateCustomerRequest("Sam", "Smith", "self@example.com", "0501112222");
        var result = await _sut.UpdateAsync(idNumber, request);

        Assert.Equal("self@example.com", result.Email);
    }

    [Fact]
    public async Task GetByIdNumberAsync_ShouldThrow_WhenCustomerNotFound()
    {
        _customerRepo.GetByIdNumberAsync("UNKNOWN").Returns((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdNumberAsync("UNKNOWN"));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateFields_WhenCustomerExists()
    {
        const string idNumber = "ID999";
        var existing = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Old",
            LastName = "Name",
            IdNumber = idNumber,
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-40)),
            Phone = "0500000000",
            CreatedAt = DateTime.UtcNow
        };
        _customerRepo.GetByIdNumberAsync(idNumber).Returns(existing);

        var request = new UpdateCustomerRequest("New", "Name", "new@email.com", "0501111111");
        var result = await _sut.UpdateAsync(idNumber, request);

        Assert.Equal("New", result.FirstName);
        Assert.Equal("new@email.com", result.Email);
        await _customerRepo.Received(1).UpdateAsync(Arg.Any<Customer>());
    }
}
