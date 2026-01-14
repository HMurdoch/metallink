using MediatR;
using MetalLink.Application.Interfaces;
using MetalLink.Shared.Tickets;

namespace MetalLink.Application.Tickets.Queries;

public sealed class SearchTicketsQueryHandler
    : IRequestHandler<SearchTicketsQuery, TicketSearchResultDto[]>
{
    private readonly ITicketRepository _ticketRepository;

    public SearchTicketsQueryHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<TicketSearchResultDto[]> Handle(
        SearchTicketsQuery query,
        CancellationToken cancellationToken)
    {
        var criteria = query.Request ?? new TicketSearchRequestDto();

        var tickets = await _ticketRepository.SearchAsync(criteria, cancellationToken);

        return tickets
            .Select(t =>
            {
                var customer = t.Customer!;
                var company = customer.Company;
                var site = customer.Site;

                return new TicketSearchResultDto
                {
                    TicketId = t.TicketId,
                    TicketNumber = t.TicketNumber,
                    TicketType = t.TicketType,
                    CustomerId = t.CustomerId ?? 0,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    CompanyName = company?.CompanyName,
                    SiteName = site?.SiteName,
                    AccountNumber = customer.AccountNumber,
                    NetWeightKg = t.NetWeightKg,
                    TotalExclVat = t.TotalAmount,
                    VatAmount = t.VatAmount,
                    TotalInclVat = t.TotalInclVat,
                    CreatedTime = t.CreatedTime
                };
            })
            .ToArray();
    }
}