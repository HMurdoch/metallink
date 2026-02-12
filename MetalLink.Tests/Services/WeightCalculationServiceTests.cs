using Xunit;
using MetalLink.Application.Services;

namespace MetalLink.Tests.Services;

public class WeightCalculationServiceTests
{
    [Fact]
    public void IsWeighbridgeTicket_WithType1_ReturnsTrue()
    {
        Assert.True(WeightCalculationService.IsWeighbridgeTicket(1));
    }

    [Fact]
    public void IsPlatformTicket_WithType2_ReturnsTrue()
    {
        Assert.True(WeightCalculationService.IsPlatformTicket(2));
    }

    [Fact]
    public void IsValidWeightPair_ForReceiving_ValidatesCorrectly()
    {
        // Receiving: first (tare) should be less than second (gross)
        Assert.True(WeightCalculationService.IsValidWeightPair(500m, 1500m, isReceiving: true));
        Assert.False(WeightCalculationService.IsValidWeightPair(1500m, 500m, isReceiving: true));
    }

    [Fact]
    public void IsValidWeightPair_ForSending_ValidatesCorrectly()
    {
        // Sending: second (gross) should be less than first (empty)
        Assert.True(WeightCalculationService.IsValidWeightPair(2000m, 1000m, isReceiving: false));
        Assert.False(WeightCalculationService.IsValidWeightPair(1000m, 2000m, isReceiving: false));
    }

    [Fact]
    public void IsValidWeightPair_WithNegativeWeights_ReturnsFalse()
    {
        Assert.False(WeightCalculationService.IsValidWeightPair(-100m, 500m, isReceiving: true));
        Assert.False(WeightCalculationService.IsValidWeightPair(500m, -100m, isReceiving: true));
    }

    [Fact]
    public void IsValidWeight_WithPositiveValue_ReturnsTrue()
    {
        Assert.True(WeightCalculationService.IsValidWeight(100m));
        Assert.True(WeightCalculationService.IsValidWeight(0.01m));
    }

    [Fact]
    public void IsValidWeight_WithZeroOrNegative_ReturnsFalse()
    {
        Assert.False(WeightCalculationService.IsValidWeight(0m));
        Assert.False(WeightCalculationService.IsValidWeight(-100m));
    }

    [Fact]
    public void ValidateTicketWeights_ForWeighbridgeReceiving_WithValidWeights_ReturnsValid()
    {
        // Arrange
        var result = WeightCalculationService.ValidateTicketWeights(
            ticketTypeId: 1,
            firstWeightKg: 500m,
            secondWeightKg: 1500m,
            isReceiving: true
        );

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateTicketWeights_ForWeighbridgeReceiving_WithInvalidWeights_ReturnsInvalid()
    {
        // Arrange
        var result = WeightCalculationService.ValidateTicketWeights(
            ticketTypeId: 1,
            firstWeightKg: 1500m,
            secondWeightKg: 500m,
            isReceiving: true
        );

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("first_weight_kg should be less than second_weight_kg", result.ErrorMessage);
    }

    [Fact]
    public void ValidateTicketWeights_ForPlatformTicket_WithNoWeights_ReturnsValid()
    {
        // Arrange
        var result = WeightCalculationService.ValidateTicketWeights(
            ticketTypeId: 2,
            firstWeightKg: null,
            secondWeightKg: null,
            isReceiving: true
        );

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateTicketWeights_ForPlatformTicket_WithWeights_ReturnsInvalid()
    {
        // Arrange
        var result = WeightCalculationService.ValidateTicketWeights(
            ticketTypeId: 2,
            firstWeightKg: 500m,
            secondWeightKg: 1500m,
            isReceiving: true
        );

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Platform tickets should not have", result.ErrorMessage);
    }

    [Fact]
    public void ValidateTicketWeights_ForWeighbridgeTicket_WithoutWeights_ReturnsInvalid()
    {
        // Arrange
        var result = WeightCalculationService.ValidateTicketWeights(
            ticketTypeId: 1,
            firstWeightKg: null,
            secondWeightKg: null,
            isReceiving: true
        );

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("require both first_weight_kg and second_weight_kg", result.ErrorMessage);
    }

    [Fact]
    public void CalculateNetWeightFromLineItems_WithMultipleItems_ReturnsSum()
    {
        // Arrange
        var weights = new List<decimal> { 100m, 200m, 150m };

        // Act
        var total = WeightCalculationService.CalculateNetWeightFromLineItems(weights);

        // Assert
        Assert.Equal(450m, total);
    }

    [Fact]
    public void CalculateNetWeightFromLineItems_WithEmptyList_ReturnsZero()
    {
        // Arrange
        var weights = new List<decimal>();

        // Act
        var total = WeightCalculationService.CalculateNetWeightFromLineItems(weights);

        // Assert
        Assert.Equal(0m, total);
    }
}
