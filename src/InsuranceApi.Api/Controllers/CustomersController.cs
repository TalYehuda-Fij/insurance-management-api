using InsuranceApi.Application.DTOs.Customer;
using InsuranceApi.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceApi.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _customerService;

    public CustomersController(CustomerService customerService) => _customerService = customerService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        var result = await _customerService.CreateAsync(request);
        return CreatedAtAction(nameof(GetByIdNumber), new { idNumber = result.IdNumber }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _customerService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{idNumber}")]
    public async Task<IActionResult> GetByIdNumber(string idNumber)
    {
        var result = await _customerService.GetByIdNumberAsync(idNumber);
        return Ok(result);
    }

    [HttpPut("{idNumber}")]
    public async Task<IActionResult> Update(string idNumber, [FromBody] UpdateCustomerRequest request)
    {
        var result = await _customerService.UpdateAsync(idNumber, request);
        return Ok(result);
    }
}
