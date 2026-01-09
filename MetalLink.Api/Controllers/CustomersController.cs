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
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CustomerDto dto,
        CancellationToken cancellationToken)
    {
        if (dto == null)
            return BadRequest("Request body is required.");

        // If client didn't send account number, generate it
        if (!dto.AccountNumber.HasValue || dto.AccountNumber.Value <= 0)
        {
            dto.AccountNumber = await _customerRepository.GetNextAccountNumberAsync(cancellationToken);
        }

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var command = new CreateCustomerCommand
        {
            CompanyId = dto.CompanyId, // long? → long?
            SiteId = dto.SiteId, // long? → long?
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IsCompany = dto.IsCompany,
            IdNumber = dto.IdNumber,
            AccountNumber = dto.AccountNumber,
            PriceCode = dto.PriceCode,
            PhoneNumber = dto.PhoneNumber,
            MobileNumber = dto.MobileNumber,
            Email = dto.Email,
            Taxable = dto.Taxable
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

        customer.FirstName = dto.FirstName;
        customer.LastName = dto.LastName;
        customer.IsCompany = dto.IsCompany;
        customer.IdNumber = dto.IdNumber;
        customer.AccountNumber = dto.AccountNumber;
        customer.PriceCode = dto.PriceCode;
        customer.PhoneNumber = dto.PhoneNumber;
        customer.MobileNumber = dto.MobileNumber;
        customer.Email = dto.Email;
        customer.Taxable = dto.Taxable;

        customer.UpdatedTime = DateTime.UtcNow;

        // ---- Update Site fields too (because your UI edits address lines, and those live on Site)
        if (customer.Site != null)
        {
            customer.Site.AddressLine1 = dto.AddressLine1;
            customer.Site.AddressLine2 = dto.AddressLine2;
            customer.Site.Suburb = dto.Suburb;
            customer.Site.City = dto.City;
            customer.Site.PostalCode = dto.PostalCode;

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
