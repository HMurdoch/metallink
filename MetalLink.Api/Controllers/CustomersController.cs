using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MetalLink.Application.Customers.Queries;
using MetalLink.Shared.Customers;
using MetalLink.Application.Interfaces;
using System.Text;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICustomerRepository _customerRepository;
    private readonly IFileStorage _fileStorage;

    public CustomersController(IMediator mediator, ICustomerRepository customerRepository, IFileStorage fileStorage)
    {
        _mediator = mediator;
        _customerRepository = customerRepository;
        _fileStorage = fileStorage;
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
            Taxable = dto.Taxable,
            
            // Image paths
            IdCardImagePath = dto.IdCardImagePath,
            DriverLicenseImagePath = dto.DriverLicenseImagePath,
            PhotoImagePath = dto.PhotoImagePath,
            SignatureImagePath = dto.SignatureImagePath,
            FingerprintImagePath = dto.FingerprintImagePath
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

        // Update image paths if provided
        if (!string.IsNullOrWhiteSpace(dto.IdCardImagePath))
            customer.IdCardImagePath = dto.IdCardImagePath;
        if (!string.IsNullOrWhiteSpace(dto.DriverLicenseImagePath))
            customer.DriverLicenseImagePath = dto.DriverLicenseImagePath;
        if (!string.IsNullOrWhiteSpace(dto.PhotoImagePath))
            customer.PhotoImagePath = dto.PhotoImagePath;
        if (!string.IsNullOrWhiteSpace(dto.SignatureImagePath))
            customer.SignatureImagePath = dto.SignatureImagePath;
        if (!string.IsNullOrWhiteSpace(dto.FingerprintImagePath))
            customer.FingerprintImagePath = dto.FingerprintImagePath;

        customer.UpdatedTime = DateTime.UtcNow;

        // Address, province and country belong to Site; update them via Site endpoints, not customer update.
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

    // -----------------------------
    // UPLOAD CUSTOMER IMAGE
    // -----------------------------
    [HttpPost("{customerId:long}/images/{imageType}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadImage(
        long customerId,
        string imageType,
        [FromBody] UploadImageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ImageData == null || request.ImageData.Length == 0)
            return BadRequest("Image data is required");

        // Validate image type
        var validTypes = new[] { "idcard", "driverlicense", "photo", "signature", "fingerprint" };
        if (!validTypes.Contains(imageType.ToLower()))
            return BadRequest($"Invalid image type. Valid types: {string.Join(", ", validTypes)}");

        // Generate storage key
        var extension = request.ContentType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/gif" => "gif",
            _ => "jpg"
        };
        
        var key = $"customers/{customerId}/{imageType}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{extension}";

        // Upload to storage
        await _fileStorage.UploadAsync(
            request.ImageData,
            request.ContentType ?? "image/jpeg",
            key,
            cancellationToken);

        // Update the customer record with the image path
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer != null)
        {
            switch (imageType.ToLower())
            {
                case "idcard":
                    customer.IdCardImagePath = key;
                    break;
                case "driverlicense":
                    customer.DriverLicenseImagePath = key;
                    break;
                case "photo":
                    customer.PhotoImagePath = key;
                    break;
                case "signature":
                    customer.SignatureImagePath = key;
                    break;
                case "fingerprint":
                    customer.FingerprintImagePath = key;
                    break;
            }
            
            customer.UpdatedTime = DateTime.UtcNow;
            await _customerRepository.UpdateAsync(customer, cancellationToken);
        }

        // Return the storage key/path
        return Ok(new { ImagePath = key });
    }

    // -----------------------------
    // DOWNLOAD CUSTOMER IMAGE
    // -----------------------------
    [HttpGet("{customerId:long}/images/{imageType}")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadImage(
        long customerId,
        string imageType,
        CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
            return NotFound("Customer not found");

        // Get the image path based on type
        string? imagePath = imageType.ToLower() switch
        {
            "idcard" => customer.IdCardImagePath,
            "driverlicense" => customer.DriverLicenseImagePath,
            "photo" => customer.PhotoImagePath,
            "signature" => customer.SignatureImagePath,
            "fingerprint" => customer.FingerprintImagePath,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(imagePath))
            return NotFound("Image not found");

        try
        {
            // Get pre-signed URL from storage
            var url = _fileStorage.GetFileUrl(imagePath, TimeSpan.FromMinutes(5));
            
            // Download the image from storage
            using var httpClient = new HttpClient();
            var imageData = await httpClient.GetByteArrayAsync(url, cancellationToken);
            
            return File(imageData, "image/png");
        }
        catch (Exception)
        {
            return NotFound("Image file not found in storage");
        }
    }
}

public sealed class UploadImageRequest
{
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string? ContentType { get; set; }
}
