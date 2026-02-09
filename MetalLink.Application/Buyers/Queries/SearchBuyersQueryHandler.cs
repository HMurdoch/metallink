using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MetalLink.Application.Interfaces;
using MetalLink.Shared.Buyers;

namespace MetalLink.Application.Buyers.Queries;

public sealed class SearchBuyersQueryHandler
    : IRequestHandler<SearchBuyersQuery, BuyerDto[]>
{
    private readonly IBuyerRepository _buyerRepository;

    public SearchBuyersQueryHandler(IBuyerRepository buyerRepository)
    {
        _buyerRepository = buyerRepository;
    }

    public async Task<BuyerDto[]> Handle(
        SearchBuyersQuery query,
        CancellationToken cancellationToken)
    {
        // Use the DTO that the query already wraps
        var criteria = query.Request ?? new BuyerSearchRequestDto();

        var buyers = await _buyerRepository.SearchAsync(criteria);

        return buyers
            .Select(b =>
            {
                var company = b.Company;
                var site = b.Site;
                var imagePath = b.ImagePath;

                return new BuyerDto
                {
                    BuyerId       = b.BuyerId,
                    FirstName     = b.FirstName,
                    LastName      = b.LastName,
                    FullName      = b.FullName,
                    IsActive      = b.IsActive,

                    CompanyId     = b.CompanyId,
                    CompanyName   = company?.CompanyName ?? string.Empty,

                    SiteId        = b.SiteId,
                    SiteName      = site?.SiteName ?? string.Empty,
                    SiteCode      = site?.SiteCode ?? string.Empty,

                    IdNumber      = b.IdNumber,
                    AccountNumber = b.AccountNumber,
                    PriceCode     = b.PriceCode,

                    PhoneNumber   = b.PhoneNumber,
                    MobileNumber  = b.MobileNumber,
                    Email         = b.Email,

                    IsActive_Display = b.IsActive,
                    IsTaxable     = b.IsTaxable,
                    CreatedTime   = b.CreatedTime,
                    UpdatedTime   = b.UpdatedTime,

                    ImagePathId = b.ImagePathId,
                    IdCardImagePath = imagePath?.IdCardImagePath,
                    DriverLicenseImagePath = imagePath?.DriverLicenseImagePath,
                    PhotoImagePath = imagePath?.PhotoImagePath,
                    SignatureImagePath = imagePath?.SignatureImagePath,
                    FingerprintImagePath = imagePath?.FingerprintImagePath
                };
            })
            .ToArray();
    }
}
