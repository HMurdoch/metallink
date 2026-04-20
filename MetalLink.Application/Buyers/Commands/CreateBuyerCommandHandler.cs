using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MetalLink.Application.Interfaces;
using MetalLink.Domain.Entities;
using MetalLink.Shared.Buyers;

namespace MetalLink.Application.Buyers.Commands;

public sealed class CreateBuyerCommandHandler
    : IRequestHandler<CreateBuyerCommand, BuyerDto?>
{
    private readonly IBuyerRepository _buyerRepository;
    private readonly IAccountNumberGenerator _accountNumberGenerator;
    private readonly IProductPriceListRepository _priceListRepository;

    public CreateBuyerCommandHandler(
        IBuyerRepository buyerRepository,
        IAccountNumberGenerator accountNumberGenerator,
        IProductPriceListRepository priceListRepository)
    {
        _buyerRepository = buyerRepository;
        _accountNumberGenerator = accountNumberGenerator;
        _priceListRepository = priceListRepository;
    }

    public async Task<BuyerDto?> Handle(
        CreateBuyerCommand request,
        CancellationToken cancellationToken)
    {
        // Generate account number if not provided
        var accountNumber = request.AccountNumber 
            ?? await _accountNumberGenerator.GetNextAsync(cancellationToken);

        var priceListId = request.ProductPriceListId;

        var now = DateTimeOffset.UtcNow;
        var buyer = new Buyer
        {
            CompanyId     = request.CompanyId ?? 0,
            SiteId        = request.SiteId ?? 0,
            FirstName     = request.FirstName,
            LastName      = request.LastName,
            IdNumber      = request.IdNumber,
            AccountNumber = accountNumber,
            ProductPriceListId = priceListId,
            PhoneNumber   = request.PhoneNumber,
            MobileNumber  = request.MobileNumber,
            Email         = request.Email,
            IsTaxable     = request.IsTaxable,
            IsActive      = true,
            CreatedTime   = now,
            UpdatedTime   = now,
            ImagePathId   = request.ImagePathId,
            CreatedByOperatorId = request.CreatedByOperatorId
        };

        await _buyerRepository.AddAsync(buyer, cancellationToken);

        var dto = new BuyerDto
        {
            BuyerId = buyer.BuyerId,
            FirstName = buyer.FirstName,
            LastName = buyer.LastName,
            FullName = buyer.FullName,

            CompanyId = buyer.CompanyId,
            SiteId = buyer.SiteId,

            IsTaxable = buyer.IsTaxable,
            Taxable   = buyer.IsTaxable,

            IdNumber      = buyer.IdNumber,
            AccountNumber = buyer.AccountNumber,
            ProductPriceListId = buyer.ProductPriceListId,
            PhoneNumber   = buyer.PhoneNumber,
            MobileNumber  = buyer.MobileNumber,
            Email         = buyer.Email,

            IsActive    = buyer.IsActive,
            CreatedTime = buyer.CreatedTime,
            UpdatedTime = buyer.UpdatedTime,
            ImagePathId = buyer.ImagePathId

            // CompanyName, SiteName, address, Province, Country, etc.
            // are intentionally left null here – they will be filled by
            // search / get-by-id queries that include those navigations.
        };

        return dto;
    }
}
