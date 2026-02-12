using MediatR;
using MetalLink.Shared.Buyers;

namespace MetalLink.Application.Buyers.Queries;

public sealed record SearchBuyersQuery(BuyerSearchRequestDto Request)
    : IRequest<BuyerDto[]>;
