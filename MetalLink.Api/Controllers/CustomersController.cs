using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MetalLink.Application.Customers.Commands;
using MetalLink.Application.Customers.Queries;
using MetalLink.Shared.Customers;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var command = new CreateCustomerCommand(
            request.SiteId,
            request.FullName,
            request.IsCompany,
            request.CompanyName,
            request.IdNumber,
            request.AccountNumber,
            request.PriceCode,
            request.AddressLine1,
            request.AddressLine2,
            request.Suburb,
            request.City,
            request.PostalCode,
            request.PhoneNumber,
            request.MobileNumber,
            request.Email);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(CustomerDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] CustomerSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        var query = new SearchCustomersQuery(request);
        var customers = await _mediator.Send(query, cancellationToken);
        return Ok(customers);
    }

    [HttpGet("{customerId:long}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long customerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(customerId), cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }


}

// DTO used when creating customers via API
public sealed class CreateCustomerRequest
{
    public long SiteId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsCompany { get; set; }
    public string? CompanyName { get; set; }
    public string? IdNumber { get; set; }
    public string? AccountNumber { get; set; }
    public string? PriceCode { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Suburb { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
}
