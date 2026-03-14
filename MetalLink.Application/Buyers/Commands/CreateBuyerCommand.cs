using MediatR;
using MetalLink.Shared.Buyers;

namespace MetalLink.Application.Buyers.Commands;

public sealed record CreateBuyerCommand : IRequest<BuyerDto?>
{
    public int? CompanyId { get; init; }
    public int? SiteId   { get; init; }

    public string? FirstName { get; init; }
    public string? LastName  { get; init; }
    public bool   IsCompany  { get; init; }

    public string? IdNumber      { get; init; }
    public long? AccountNumber { get; init; }
    public int? ProductPriceListId { get; init; }
    public string? PriceCode     { get; init; }

    public string? PhoneNumber   { get; init; }
    public string? MobileNumber  { get; init; }
    public string? Email         { get; init; }

    public bool IsTaxable { get; init; } = true;

    public int? ImagePathId { get; init; }

    public int CreatedByOperatorId { get; init; } = 1;
}
