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
            CompanyId = dto.CompanyId,
            SiteId = dto.SiteId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IsCompany = dto.IsCompany,
            IdNumber = dto.IdNumber,
            AccountNumber = dto.AccountNumber,
            PriceCode = dto.PriceCode,
            PhoneNumber = dto.PhoneNumber,
            MobileNumber = dto.MobileNumber,
            Email = dto.Email,
            IsTaxable = dto.Taxable
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
    [HttpGet("{customerId:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int customerId,
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
        customer.IsTaxable = dto.Taxable;

        customer.UpdatedTime = DateTimeOffset.UtcNow;

        // Address, province and country belong to Site; update them via Site endpoints, not customer update.
        await _customerRepository.UpdateAsync(customer, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{customerId:int}")]
    public async Task<IActionResult> SoftDelete(int customerId, CancellationToken cancellationToken)
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
    [HttpPost("{customerId:int}/images/{imageType}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadImage(
        int customerId,
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
        
        var key = $"customers/{customerId}/{imageType}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.{extension}";

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
            // Create or update ImagePath entity
            if (customer.ImagePath == null)
            {
                customer.ImagePath = new MetalLink.Domain.Entities.ImagePath();
            }

            switch (imageType.ToLower())
            {
                case "idcard":
                    customer.ImagePath.IdCardImagePath = key;
                    break;
                case "driverlicense":
                    customer.ImagePath.DriverLicenseImagePath = key;
                    break;
                case "photo":
                    customer.ImagePath.PhotoImagePath = key;
                    break;
                case "signature":
                    customer.ImagePath.SignatureImagePath = key;
                    break;
                case "fingerprint":
                    customer.ImagePath.FingerprintImagePath = key;
                    break;
            }
            
            customer.UpdatedTime = DateTimeOffset.UtcNow;
            await _customerRepository.UpdateAsync(customer, cancellationToken);
        }

        // Return the storage key/path
        return Ok(new { ImagePath = key });
    }

    // -----------------------------
    // DOWNLOAD CUSTOMER IMAGE
    // -----------------------------
    [HttpGet("{customerId:int}/images/{imageType}")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadImage(
        int customerId,
        string imageType,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"[API] DownloadImage called: customerId={customerId}, imageType={imageType}");
        
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
        {
            Console.WriteLine($"[API] Customer {customerId} not found");
            return NotFound("Customer not found");
        }

        // Get the image path based on type from ImagePath entity
        if (customer.ImagePath == null)
        {
            Console.WriteLine($"[API] ImagePath not found for customer {customerId}");
            return NotFound("Image not found");
        }

        string? imagePath = imageType.ToLower() switch
        {
            "idcard" => customer.ImagePath.IdCardImagePath,
            "driverlicense" => customer.ImagePath.DriverLicenseImagePath,
            "photo" => customer.ImagePath.PhotoImagePath,
            "signature" => customer.ImagePath.SignatureImagePath,
            "fingerprint" => customer.ImagePath.FingerprintImagePath,
            _ => null
        };

        Console.WriteLine($"[API] Image path for {imageType}: {imagePath}");

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            Console.WriteLine($"[API] Image path is null or empty");
            return NotFound("Image not found");
        }

        try
        {
            // Get pre-signed URL from storage
            Console.WriteLine($"[API] Getting pre-signed URL for {imagePath}");
            var url = _fileStorage.GetFileUrl(imagePath, TimeSpan.FromMinutes(5));
            Console.WriteLine($"[API] Pre-signed URL: {url}");
            
            // Download the image from storage
            using var httpClient = new HttpClient();
            Console.WriteLine($"[API] Downloading image from storage...");
            var imageData = await httpClient.GetByteArrayAsync(url, cancellationToken);
            Console.WriteLine($"[API] Downloaded {imageData.Length} bytes");
            
            return File(imageData, "image/png");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API] Error downloading image: {ex.Message}");
            return NotFound("Image file not found in storage");
        }
    }
}

public sealed class UploadImageRequest
{
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string? ContentType { get; set; }
}
