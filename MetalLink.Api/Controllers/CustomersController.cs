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
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
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
            request.Email
        );

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetCustomerById),
            new { id = result.CustomerId }, result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerById(long id, CancellationToken cancellationToken)
    {
        var query = new GetCustomerByIdQuery(id);
        var customer = await _mediator.Send(query, cancellationToken);

        if (customer is null)
        {
            return NotFound();
        }

        return Ok(customer);
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
