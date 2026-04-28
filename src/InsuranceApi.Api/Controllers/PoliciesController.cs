using InsuranceApi.Application.DTOs.Policy;
using InsuranceApi.Application.Services;
using InsuranceApi.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceApi.Api.Controllers;

[ApiController]
public class PoliciesController : ControllerBase
{
    private readonly PolicyService _policyService;

    public PoliciesController(PolicyService policyService) => _policyService = policyService;

    [HttpPost("api/customers/{idNumber}/policies")]
    public async Task<IActionResult> Create(string idNumber, [FromBody] CreatePolicyRequest request, CancellationToken cancellationToken)
    {
        var result = await _policyService.CreateAsync(idNumber, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("api/policies")]
    public async Task<IActionResult> GetAll([FromQuery] PolicyType? type, [FromQuery] PolicyStatus? status, [FromQuery] string? idNumber, CancellationToken cancellationToken)
    {
        var result = await _policyService.GetAllAsync(type, status, idNumber, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/policies/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _policyService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("api/policies/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePolicyRequest request, CancellationToken cancellationToken)
    {
        var result = await _policyService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("api/policies/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _policyService.CancelAsync(id, cancellationToken);
        return Ok(result);
    }
}
