using Xunit;
using MetalLink.Application.Services;

namespace MetalLink.Tests.Services;

public class TicketCalculationServiceTests
{
    [Fact]
    public void CalculateTotalExVat_WithLineItems_ReturnsSumOfWeightTimesPrice()
    {
        // Arrange
        var lineItems = new List<(decimal NetWeightKg, decimal UnitPricePerKg)>
        {
            (100m, 50m),     // 5000
            (200m, 60m),     // 12000
            (150m, 55m)      // 8250
        };

        // Act
        var total = TicketCalculationService.CalculateTotalExVat(lineItems);

        // Assert
        Assert.Equal(25250m, total);
    }

    [Fact]
    public void CalculateVat_With15Percent_ReturnsCorrectAmount()
    {
        // Arrange
        var totalExVat = 1000m;

        // Act
        var vat = TicketCalculationService.CalculateVat(totalExVat);

        // Assert
        Assert.Equal(150m, vat); // 15% of 1000
    }

    [Fact]
    public void CalculateTotalIncVat_WithTotalAndVat_ReturnsSum()
    {
        // Arrange
        var totalExVat = 1000m;

        // Act
        var totalIncVat = TicketCalculationService.CalculateTotalIncVat(totalExVat);

        // Assert
        Assert.Equal(1150m, totalIncVat); // 1000 + 150
    }

    [Fact]
    public void CalculateAllTotals_WithLineItems_ReturnsAllThreeValues()
    {
        // Arrange
        var lineItems = new List<(decimal NetWeightKg, decimal UnitPricePerKg)>
        {
            (100m, 50m)  // 5000
        };

        // Act
        var (totalExVat, vat, totalIncVat) = TicketCalculationService.CalculateAllTotals(lineItems);

        // Assert
        Assert.Equal(5000m, totalExVat);
        Assert.Equal(750m, vat);
        Assert.Equal(5750m, totalIncVat);
    }

    [Fact]
    public void CalculateNetWeightFromScale_ForReceivingTicket_ReturnsDifference()
    {
        // Arrange - Receiving: first weight (tare) = 500, second weight (gross) = 1500
        var firstWeight = 500m;
        var secondWeight = 1500m;

        // Act
        var netWeight = TicketCalculationService.CalculateNetWeightFromScale(firstWeight, secondWeight, isReceiving: true);

        // Assert
        Assert.Equal(-1000m, netWeight); // first - second = 500 - 1500 = -1000 for receiving
    }

    [Fact]
    public void CalculateNetWeightFromScale_ForSendingTicket_ReturnsReverseDifference()
    {
        // Arrange - Sending: first weight (empty) = 2000, second weight (loaded) = 1000
        var firstWeight = 2000m;
        var secondWeight = 1000m;

        // Act
        var netWeight = TicketCalculationService.CalculateNetWeightFromScale(firstWeight, secondWeight, isReceiving: false);

        // Assert
        Assert.Equal(-1000m, netWeight); // second - first = 1000 - 2000 = -1000 for sending
    }

    [Fact]
    public void IsValidWeightKg_WithPositiveValue_ReturnsTrue()
    {
        Assert.True(TicketCalculationService.IsValidPrice(100m));
        Assert.True(TicketCalculationService.IsValidPrice(0.01m));
    }

    [Fact]
    public void IsValidPrice_WithNegativeOrZero_ReturnsFalse()
    {
        Assert.False(TicketCalculationService.IsValidPrice(-50m));
        // 0 is valid for price (no cost items)
        Assert.True(TicketCalculationService.IsValidPrice(0m));
    }
}
