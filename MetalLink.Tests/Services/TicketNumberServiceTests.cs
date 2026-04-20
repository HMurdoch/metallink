using MetalLink.Application.Interfaces;
using MetalLink.Application.Services;
using Xunit;

namespace MetalLink.Tests.Services;

public class TicketNumberServiceTests
{
    private sealed class FakeReceivingRepo : ITicketReceivingRepository
    {
        public long NextValue { get; set; }
        public long PeekValue { get; set; }

        public Task<long> GetNextTicketSequenceValueAsync(string prefix) => Task.FromResult(NextValue);
        public Task<long> PeekNextTicketSequenceValueAsync(string prefix) => Task.FromResult(PeekValue);

        // Unused members for these tests
        public Task<MetalLink.Domain.Entities.TicketReceiving?> GetByIdAsync(int ticketReceivingId) => Task.FromResult<MetalLink.Domain.Entities.TicketReceiving?>(null);
        public Task<MetalLink.Domain.Entities.TicketReceiving?> GetByTicketNumberAsync(string ticketNumber) => Task.FromResult<MetalLink.Domain.Entities.TicketReceiving?>(null);
        public Task<IEnumerable<MetalLink.Domain.Entities.TicketReceiving>> SearchAsync(string? searchTerm = null, int? companyId = null, int? siteId = null, int? customerId = null, string? firstName = null, string? lastName = null, string? idNumber = null, long? accountNumber = null, int? productId = null, string? ticketType = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? deliveryStatus = null, int pageNumber = 1, int pageSize = 50) => Task.FromResult<IEnumerable<MetalLink.Domain.Entities.TicketReceiving>>(new List<MetalLink.Domain.Entities.TicketReceiving>());
        public Task<long> GetCountAsync(string? searchTerm = null, int? companyId = null, int? siteId = null, int? customerId = null, string? firstName = null, string? lastName = null, string? idNumber = null, long? accountNumber = null, int? productId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? deliveryStatus = null) => Task.FromResult(0L);
        public Task<MetalLink.Domain.Entities.TicketReceiving> AddAsync(MetalLink.Domain.Entities.TicketReceiving ticket) => Task.FromResult(ticket);
        public Task UpdateAsync(MetalLink.Domain.Entities.TicketReceiving ticket) => Task.CompletedTask;
        public Task<string> GenerateTicketNumberAsync(int siteId) => Task.FromResult(string.Empty);
        public Task<string?> GetLastTicketNumberByPrefixAsync(string prefix) => Task.FromResult<string?>(null);
    }

    private sealed class FakeSendingRepo : ITicketSendingRepository
    {
        public long NextValue { get; set; }
        public long PeekValue { get; set; }

        public Task<long> GetNextTicketSequenceValueAsync(string prefix) => Task.FromResult(NextValue);
        public Task<long> PeekNextTicketSequenceValueAsync(string prefix) => Task.FromResult(PeekValue);

        // Unused members for these tests
        public Task<MetalLink.Domain.Entities.TicketSending?> GetByIdAsync(int ticketSendingId) => Task.FromResult<MetalLink.Domain.Entities.TicketSending?>(null);
        public Task<MetalLink.Domain.Entities.TicketSending?> GetByTicketNumberAsync(string ticketNumber) => Task.FromResult<MetalLink.Domain.Entities.TicketSending?>(null);
        public Task<IEnumerable<MetalLink.Domain.Entities.TicketSending>> SearchAsync(string? searchTerm = null, int? companyId = null, int? siteId = null, int? buyerId = null, string? firstName = null, string? lastName = null, string? idNumber = null, long? accountNumber = null, int? productId = null, string? ticketType = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? deliveryStatus = null, int pageNumber = 1, int pageSize = 50) => Task.FromResult<IEnumerable<MetalLink.Domain.Entities.TicketSending>>(new List<MetalLink.Domain.Entities.TicketSending>());
        public Task<long> GetCountAsync(string? searchTerm = null, int? companyId = null, int? siteId = null, int? buyerId = null, string? firstName = null, string? lastName = null, string? idNumber = null, long? accountNumber = null, int? productId = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? deliveryStatus = null) => Task.FromResult(0L);
        public Task<MetalLink.Domain.Entities.TicketSending> AddAsync(MetalLink.Domain.Entities.TicketSending ticket) => Task.FromResult(ticket);
        public Task UpdateAsync(MetalLink.Domain.Entities.TicketSending ticket) => Task.CompletedTask;
        public Task<string> GenerateTicketNumberAsync(int siteId) => Task.FromResult(string.Empty);
        public Task<string?> GetLastTicketNumberByPrefixAsync(string prefix) => Task.FromResult<string?>(null);
        public Task<HashSet<int>> GetBuyerIdsWithActiveTicketsAsync(int? companyId = null, int? siteId = null, CancellationToken ct = default) => Task.FromResult(new HashSet<int>());
    }

    [Fact]
    public async Task PeekNextReceivingTicketNumberAsync_FormatsWithPrefixAndPadding()
    {
        var receivingRepo = new FakeReceivingRepo { PeekValue = 34 };
        var service = new TicketNumberService(receivingRepo, new FakeSendingRepo());

        var ticketNumber = await service.PeekNextReceivingTicketNumberAsync(1);

        Assert.Equal("RWB-00000034", ticketNumber);
    }

    [Fact]
    public void IsValidPriceCode_WithValidCodes_ReturnsTrue()
    {
        Assert.True(PriceLookupService.IsValidPriceCode("A"));
        Assert.True(PriceLookupService.IsValidPriceCode("B"));
        Assert.True(PriceLookupService.IsValidPriceCode("C"));
        Assert.True(PriceLookupService.IsValidPriceCode("a"));
    }

    [Fact]
    public void IsValidPriceCode_WithInvalidCodes_ReturnsFalse()
    {
        Assert.False(PriceLookupService.IsValidPriceCode("D"));
        Assert.False(PriceLookupService.IsValidPriceCode(string.Empty));
        Assert.False(PriceLookupService.IsValidPriceCode(null));
        Assert.False(PriceLookupService.IsValidPriceCode("AB"));
    }
}
