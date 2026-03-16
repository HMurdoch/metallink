using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MetalLink.Application.Buyers.Commands;
using MetalLink.Application.Buyers.Queries;
using MetalLink.Shared.Buyers;
using MetalLink.Application.Interfaces;
using MetalLink.Api.Extensions;
using System.Text;

namespace MetalLink.Api.Controllers;

[ApiController]
[Route("api/buyers")]
[Authorize]
public sealed class BuyersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IBuyerRepository _buyerRepository;
    private readonly IFileStorage _fileStorage;

    public BuyersController(IMediator mediator, IBuyerRepository buyerRepository, IFileStorage fileStorage)
    {
        _mediator = mediator;
        _buyerRepository = buyerRepository;
        _fileStorage = fileStorage;
    }

    // -----------------------------
    // CREATE BUYER
    // -----------------------------
    [HttpPost]
    [ProducesResponseType(typeof(BuyerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] BuyerDto dto,
        CancellationToken cancellationToken)
    {
        // If client didn't send account number, generate it
        if (!dto.AccountNumber.HasValue || dto.AccountNumber.Value == 0)
        {
            dto.AccountNumber = await _buyerRepository.GetNextAccountNumberAsync(cancellationToken);
        }

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var command = new CreateBuyerCommand
        {
            CompanyId = dto.CompanyId,
            SiteId = dto.SiteId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IsCompany = dto.IsCompany,
            IdNumber = dto.IdNumber,
            AccountNumber = dto.AccountNumber,
            ProductPriceListId = dto.ProductPriceListId,
            PhoneNumber = dto.PhoneNumber,
            MobileNumber = dto.MobileNumber,
            Email = dto.Email,
            IsTaxable = dto.IsTaxable,
            CreatedByOperatorId = (int)User.GetOperatorId()
        };

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    // -----------------------------
    // SEARCH (POST body)
    // -----------------------------
    [HttpPost("search")]
    public async Task<ActionResult<BuyerDto[]>> Search(
        [FromBody] BuyerSearchRequestDto requestDto)
    {
        var result = await _mediator.Send(new SearchBuyersQuery(requestDto));
        return Ok(result);
    }

    // -----------------------------
    // SEARCH (GET query string)
    // -----------------------------
    [HttpGet("search")]
    [ProducesResponseType(typeof(BuyerDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] BuyerSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        var query = new SearchBuyersQuery(request);
        var buyers = await _mediator.Send(query, cancellationToken);
        return Ok(buyers);
    }

    // -----------------------------
    // GET BY ID
    // -----------------------------
    [HttpGet("{buyerId:int}")]
    [ProducesResponseType(typeof(BuyerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int buyerId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetBuyerByIdQuery(buyerId),
            cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    // -----------------------------
    // UPDATE BUYER
    // PUT api/buyers
    // -----------------------------
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromBody] BuyerDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.BuyerId <= 0)
            return BadRequest("BuyerId is required.");

        var buyer = await _buyerRepository.GetByIdAsync(dto.BuyerId, cancellationToken);
        if (buyer == null)
            return NotFound();

        // ---- Update Buyer fields
        if (dto.CompanyId.HasValue)
            buyer.CompanyId = dto.CompanyId.Value;

        if (dto.SiteId.HasValue)
            buyer.SiteId = dto.SiteId.Value;

        buyer.FirstName = dto.FirstName;
        buyer.LastName = dto.LastName;
        // Domain Buyer does not have IsCompany (that flag is only on the DTO)
        // If you need this behavior, we should add it to the domain model + migration.
        // For now we ignore it.
        // buyer.IsCompany = dto.IsCompany;
        buyer.IdNumber = dto.IdNumber;
        buyer.AccountNumber = dto.AccountNumber;
        buyer.ProductPriceListId = dto.ProductPriceListId;
        buyer.PhoneNumber = dto.PhoneNumber;
        buyer.MobileNumber = dto.MobileNumber;
        buyer.Email = dto.Email;
        buyer.IsTaxable = dto.IsTaxable;

        buyer.UpdatedTime = DateTimeOffset.UtcNow;

        // Address, province and country belong to Site; update them via Site endpoints, not buyer update.
        await _buyerRepository.UpdateAsync(buyer, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{buyerId:int}")]
    public async Task<IActionResult> SoftDelete(int buyerId, CancellationToken cancellationToken)
    {
        await _buyerRepository.SoftDeleteAsync(buyerId, cancellationToken);
        return NoContent();
    }

    [HttpGet("next-account-number")]
    public async Task<ActionResult<long>> GetNextAccountNumber(CancellationToken ct)
    {
        var next = await _buyerRepository.GetNextAccountNumberAsync(ct);
        return Ok(next);
    }

    // -----------------------------
    // UPLOAD CUSTOMER IMAGE
    // -----------------------------
    [HttpPost("{buyerId:int}/images/{imageType}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadImage(
        int buyerId,
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
        
        var key = $"buyers/{buyerId}/{imageType}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.{extension}";

        // Upload to storage
        await _fileStorage.UploadAsync(
            request.ImageData,
            request.ContentType ?? "image/jpeg",
            key,
            cancellationToken);

        // Update the buyer record with the image path
        var buyer = await _buyerRepository.GetByIdAsync(buyerId, cancellationToken);
        if (buyer != null)
        {
            // Create or update ImagePath entity
            if (buyer.ImagePath == null)
            {
                var now = DateTimeOffset.UtcNow;
                buyer.ImagePath = new MetalLink.Domain.Entities.ImagePath
                {
                    CreatedByOperatorId = 1, // System operator - always exists
                    CreatedTime = now,
                    UpdatedTime = now,
                    IsActive = true
                };
            }

            switch (imageType.ToLower())
            {
                case "idcard":
                    buyer.ImagePath.IdCardImagePath = key;
                    break;
                case "driverlicense":
                    buyer.ImagePath.DriverLicenseImagePath = key;
                    break;
                case "photo":
                    buyer.ImagePath.PhotoImagePath = key;
                    break;
                case "signature":
                    buyer.ImagePath.SignatureImagePath = key;
                    break;
                case "fingerprint":
                    buyer.ImagePath.FingerprintImagePath = key;
                    break;
            }
            
            buyer.UpdatedTime = DateTimeOffset.UtcNow;
            await _buyerRepository.UpdateAsync(buyer, cancellationToken);
        }

        // Return the storage key/path
        return Ok(new { ImagePath = key });
    }

    // -----------------------------
    // DOWNLOAD CUSTOMER IMAGE
    // -----------------------------
    [HttpGet("{buyerId:int}/images/{imageType}")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadImage(
        int buyerId,
        string imageType,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"[API] DownloadImage called: buyerId={buyerId}, imageType={imageType}");
        
        var buyer = await _buyerRepository.GetByIdAsync(buyerId, cancellationToken);
        if (buyer == null)
        {
            Console.WriteLine($"[API] Buyer {buyerId} not found");
            return NotFound("Buyer not found");
        }

        // Get the image path based on type from ImagePath entity
        if (buyer.ImagePath == null)
        {
            Console.WriteLine($"[API] ImagePath not found for buyer {buyerId}");
            return NotFound("Image not found");
        }

        string? imagePath = imageType.ToLower() switch
        {
            "idcard" => buyer.ImagePath.IdCardImagePath,
            "driverlicense" => buyer.ImagePath.DriverLicenseImagePath,
            "photo" => buyer.ImagePath.PhotoImagePath,
            "signature" => buyer.ImagePath.SignatureImagePath,
            "fingerprint" => buyer.ImagePath.FingerprintImagePath,
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
