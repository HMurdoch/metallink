using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MetalLink.Application.Customers.Commands;
using MetalLink.Application.Customers.Queries;
using MetalLink.Shared.Customers;
using MetalLink.Application.Interfaces;
using System.Text.Json.Serialization;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICustomerRepository _customerRepository;

    public CustomersController(IMediator mediator, ICustomerRepository customerRepository)
    {
        _mediator = mediator;
        _customerRepository = customerRepository;
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
        if (request == null)
            return BadRequest("Request body is required.");

        // If client didn't send account number, generate it
        if (!request.AccountNumber.HasValue || request.AccountNumber.Value <= 0)
        {
            request.AccountNumber = await _customerRepository.GetNextAccountNumberAsync(cancellationToken);
        }

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
            Email         = request.Email,
            Taxable       = request.Taxable
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

    // -----------------------------
    // UPDATE CUSTOMER
    // PUT api/customers
    // -----------------------------
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromBody] CustomerDto dto,
        CancellationToken cancellationToken)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        if (dto.CustomerId <= 0)
            return BadRequest("CustomerId is required.");

        var customer = await _customerRepository.GetByIdAsync(dto.CustomerId, cancellationToken);
        if (customer == null)
            return NotFound();

        // ---- Update Customer fields
        if (dto.CompanyId.HasValue)
            customer.CompanyId = dto.CompanyId.Value;

        if (dto.SiteId.HasValue)
            customer.SiteId = dto.SiteId.Value;

        customer.FirstName     = dto.FirstName;
        customer.LastName      = dto.LastName;
        customer.IsCompany     = dto.IsCompany;
        customer.IdNumber      = dto.IdNumber;
        customer.AccountNumber = dto.AccountNumber;
        customer.PriceCode     = dto.PriceCode;
        customer.PhoneNumber   = dto.PhoneNumber;
        customer.MobileNumber  = dto.MobileNumber;
        customer.Email         = dto.Email;
        customer.Taxable       = dto.Taxable;

        customer.UpdatedTime = DateTime.UtcNow;

        // ---- Update Site fields too (because your UI edits address lines, and those live on Site)
        if (customer.Site != null)
        {
            customer.Site.AddressLine1 = dto.AddressLine1;
            customer.Site.AddressLine2 = dto.AddressLine2;
            customer.Site.Suburb       = dto.Suburb;
            customer.Site.City         = dto.City;
            customer.Site.PostalCode   = dto.PostalCode;

            // DTO uses long? but Site uses int?
            if (dto.ProvinceId.HasValue)
                customer.Site.ProvinceId = (int)dto.ProvinceId.Value;

            if (dto.CountryId.HasValue)
                customer.Site.CountryId = (int)dto.CountryId.Value;

            customer.Site.UpdatedTime = DateTime.UtcNow;
        }

        await _customerRepository.UpdateAsync(customer, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{customerId:long}")]
    public async Task<IActionResult> SoftDelete(long customerId, CancellationToken cancellationToken)
    {
        await _customerRepository.SoftDeleteAsync(customerId, cancellationToken);
        return NoContent();
    }

    [HttpGet("next-account-number")]
    public async Task<ActionResult<long>> GetNextAccountNumber(CancellationToken ct)
    {
        var next = await _customerRepository.GetNextAccountNumberAsync(ct);
        return Ok(next);
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
    public long SiteId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public bool   IsCompany { get; set; }

    // Kept for now in case you still send it from UI when capturing companies
    public string? CompanyName  { get; set; }

    public string? IdNumber      { get; set; }
    
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long? AccountNumber { get; set; }
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
    public bool Taxable { get; set; } = true;

}
