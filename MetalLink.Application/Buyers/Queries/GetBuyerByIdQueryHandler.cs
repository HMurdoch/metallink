using MediatR;
using MetalLink.Application.Interfaces;
using MetalLink.Shared.Buyers;
using MetalLink.Domain.Entities;

namespace MetalLink.Application.Buyers.Queries;

public sealed class GetBuyerByIdQueryHandler
    : IRequestHandler<GetBuyerByIdQuery, BuyerDto?>
{
    private readonly IBuyerRepository _buyerRepository;

    public GetBuyerByIdQueryHandler(IBuyerRepository buyerRepository)
    {
        _buyerRepository = buyerRepository;
    }

    public async Task<BuyerDto?> Handle(
        GetBuyerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var buyer = await _buyerRepository.GetByIdAsync(request.BuyerId);

        if (buyer is null)
            return null;

        var company = buyer.Company;
        var site = buyer.Site;
        var imagePath = buyer.ImagePath;

        var dto = new BuyerDto
        {
            BuyerId       = buyer.BuyerId,
            FirstName     = buyer.FirstName,
            LastName      = buyer.LastName,
            FullName      = buyer.FullName,
            IsActive      = buyer.IsActive,

            CompanyId     = buyer.CompanyId,
            CompanyName   = company?.CompanyName ?? string.Empty,

            SiteId        = buyer.SiteId,
            SiteName      = site?.SiteName ?? string.Empty,
            SiteCode      = site?.SiteCode ?? string.Empty,

            IdNumber      = buyer.IdNumber,
            AccountNumber = buyer.AccountNumber,
            ProductPriceListId = buyer.ProductPriceListId,
            ProductPriceListName = buyer.ProductPriceList?.ProductPriceListName,

            PhoneNumber   = buyer.PhoneNumber,
            MobileNumber  = buyer.MobileNumber,
            Email         = buyer.Email,

            IsActive_Display = buyer.IsActive,
            IsTaxable     = buyer.IsTaxable,
            CreatedTime   = buyer.CreatedTime,
            UpdatedTime   = buyer.UpdatedTime,

            ImagePathId = buyer.ImagePathId,
            IdCardImagePath = imagePath?.IdCardImagePath,
            DriverLicenseImagePath = imagePath?.DriverLicenseImagePath,
            PhotoImagePath = imagePath?.PhotoImagePath,
            SignatureImagePath = imagePath?.SignatureImagePath,
            FingerprintImagePath = imagePath?.FingerprintImagePath
        };

        return dto;
    }
}
