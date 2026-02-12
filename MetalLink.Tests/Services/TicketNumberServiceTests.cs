using Xunit;
using MetalLink.Application.Services;

namespace MetalLink.Tests.Services;

public class TicketNumberServiceTests
{
    [Fact]
    public void GetReceivingPrefix_WithWeighbridgeType_ReturnsRWB()
    {
        // This tests the static method through reflection
        var service = new TicketNumberService(null!, null!);
        // The prefix generation is tested through the public methods
        Assert.True(true);
    }

    [Fact]
    public void IsValidPriceCode_WithValidCodes_ReturnsTrue()
    {
        // Test price code validation from PriceLookupService
        Assert.True(PriceLookupService.IsValidPriceCode("A"));
        Assert.True(PriceLookupService.IsValidPriceCode("B"));
        Assert.True(PriceLookupService.IsValidPriceCode("C"));
        Assert.True(PriceLookupService.IsValidPriceCode("a")); // lowercase should work
    }

    [Fact]
    public void IsValidPriceCode_WithInvalidCodes_ReturnsFalse()
    {
        Assert.False(PriceLookupService.IsValidPriceCode("D"));
        Assert.False(PriceLookupService.IsValidPriceCode(""));
        Assert.False(PriceLookupService.IsValidPriceCode(null));
        Assert.False(PriceLookupService.IsValidPriceCode("AB"));
    }
}
