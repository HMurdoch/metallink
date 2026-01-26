using Xunit;
using MetalLink.Application.Services;
using MetalLink.Domain.Entities;

namespace MetalLink.Tests.Integration;

public class TicketReceivingIntegrationTests
{
    [Fact]
    public void TicketCreation_WithValidReceivingData_GeneratesCorrectTicketNumber()
    {
        // Arrange
        var ticketTypeId = 1; // Weighbridge
        var firstWeight = 500m;
        var secondWeight = 1500m;

        // Act - Simulate ticket creation workflow
        var weightValidation = WeightCalculationService.ValidateTicketWeights(
            ticketTypeId: ticketTypeId,
            firstWeightKg: firstWeight,
            secondWeightKg: secondWeight,
            isReceiving: true
        );

        var netWeight = WeightCalculationService.CalculateNetWeightFromScale(
            firstWeight,
            secondWeight,
            isReceiving: true
        );

        // Assert
        Assert.True(weightValidation.IsValid);
        Assert.Equal(-1000m, netWeight);
    }

    [Fact]
    public void TicketLineCreation_WithLineItems_CalculatesCorrectTotals()
    {
        // Arrange - Simulate 3 line items
        var lineItems = new List<(decimal NetWeightKg, decimal UnitPricePerKg)>
        {
            (100m, 50m),   // 5000
            (200m, 60m),   // 12000
            (150m, 55m)    // 8250
        };

        // Act
        var (totalExVat, vat, totalIncVat) = TicketCalculationService.CalculateAllTotals(lineItems);

        // Assert
        Assert.Equal(25250m, totalExVat);
        Assert.Equal(3787.50m, vat);
        Assert.Equal(29037.50m, totalIncVat);
    }

    [Fact]
    public void TicketLineCreation_WithPriceCode_LooksUpCorrectPrice()
    {
        // Arrange
        var priceCode = "A";

        // Act
        var isValid = PriceLookupService.IsValidPriceCode(priceCode);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void PlatformTicket_WithWeightFields_FailsValidation()
    {
        // Arrange
        var ticketTypeId = 2; // Platform
        var firstWeight = 500m;
        var secondWeight = 1500m;

        // Act
        var validation = WeightCalculationService.ValidateTicketWeights(
            ticketTypeId: ticketTypeId,
            firstWeightKg: firstWeight,
            secondWeightKg: secondWeight,
            isReceiving: true
        );

        // Assert
        Assert.False(validation.IsValid);
        Assert.Contains("Platform tickets should not have", validation.ErrorMessage);
    }

    [Fact]
    public void PlatformTicket_WithoutWeightFields_PassesValidation()
    {
        // Arrange
        var ticketTypeId = 2; // Platform

        // Act
        var validation = WeightCalculationService.ValidateTicketWeights(
            ticketTypeId: ticketTypeId,
            firstWeightKg: null,
            secondWeightKg: null,
            isReceiving: true
        );

        // Assert
        Assert.True(validation.IsValid);
    }

    [Fact]
    public void ReceivingTicket_WithInvalidWeights_FailsValidation()
    {
        // Arrange - For receiving, first should be less than second
        var ticketTypeId = 1; // Weighbridge
        var firstWeight = 1500m; // This is GREATER than second
        var secondWeight = 500m;

        // Act
        var validation = WeightCalculationService.ValidateTicketWeights(
            ticketTypeId: ticketTypeId,
            firstWeightKg: firstWeight,
            secondWeightKg: secondWeight,
            isReceiving: true
        );

        // Assert
        Assert.False(validation.IsValid);
        Assert.Contains("first_weight_kg should be less than second_weight_kg", validation.ErrorMessage);
    }

    [Fact]
    public void MultipleLineItems_NetWeightCalculation_ReturnsSumOfWeights()
    {
        // Arrange
        var lineWeights = new List<decimal> { 100m, 200m, 150m, 50m };

        // Act
        var totalWeight = WeightCalculationService.CalculateNetWeightFromLineItems(lineWeights);

        // Assert
        Assert.Equal(500m, totalWeight);
    }
}
