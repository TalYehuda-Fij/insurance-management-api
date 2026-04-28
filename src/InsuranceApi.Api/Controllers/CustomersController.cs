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
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await _customerService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdNumber), new { idNumber = result.IdNumber }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _customerService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{idNumber}")]
    public async Task<IActionResult> GetByIdNumber(string idNumber, CancellationToken cancellationToken)
    {
        var result = await _customerService.GetByIdNumberAsync(idNumber, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{idNumber}")]
    public async Task<IActionResult> Update(string idNumber, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await _customerService.UpdateAsync(idNumber, request, cancellationToken);
        return Ok(result);
    }
}
