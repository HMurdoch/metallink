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

    // -----------------------------
    // CREATE CUSTOMER
    // -----------------------------
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // IMPORTANT: use object-initializer to match the record definition
        var command = new CreateCustomerCommand
        {
            CompanyId    = request.CompanyId,
            SiteId       = request.SiteId,
            FirstName    = request.FirstName,
            LastName     = request.LastName,
            IsCompany    = request.IsCompany,
            IdNumber     = request.IdNumber,
            AccountNumber = request.AccountNumber,
            PriceCode     = request.PriceCode,
            PhoneNumber   = request.PhoneNumber,
            MobileNumber  = request.MobileNumber,
            Email         = request.Email
        };

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    // -----------------------------
    // SEARCH (POST body)
    // -----------------------------
    [HttpPost("search")]
    public async Task<ActionResult<CustomerDto[]>> Search(
        [FromBody] CustomerSearchRequestDto requestDto)
    {
        var result = await _mediator.Send(new SearchCustomersQuery(requestDto));
        return Ok(result);
    }

    // -----------------------------
    // SEARCH (GET query string)
    // -----------------------------
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

    // -----------------------------
    // GET BY ID
    // -----------------------------
    [HttpGet("{customerId:long}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        long customerId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCustomerByIdQuery(customerId),
            cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}

// ---------------------------------------------------------------------
// DTO used when creating customers via API
// ---------------------------------------------------------------------
public sealed class CreateCustomerRequest
{
    // NEW: link to company (required in your new design)
    public long CompanyId { get; set; }

    // Optional branch / site (align with domain: long?)
    public long? SiteId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public bool   IsCompany { get; set; }

    // Kept for now in case you still send it from UI when capturing companies
    public string? CompanyName  { get; set; }

    public string? IdNumber      { get; set; }
    public string? AccountNumber { get; set; }
    public string? PriceCode     { get; set; }

    // Address fields are no longer on Customer in the DB/schema,
    // but we keep them here for now in case you still use them
    // when creating/updating companies/sites from the UI.
    public string? AddressLine1  { get; set; }
    public string? AddressLine2  { get; set; }
    public string? Suburb        { get; set; }
    public string? City          { get; set; }
    public string? PostalCode    { get; set; }

    public string? PhoneNumber   { get; set; }
    public string? MobileNumber  { get; set; }
    public string? Email         { get; set; }
}
