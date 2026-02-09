using MediatR;
using MetalLink.Shared.Buyers;

namespace MetalLink.Application.Buyers.Queries;

public sealed record GetBuyerByIdQuery(int BuyerId) : IRequest<BuyerDto?>;
