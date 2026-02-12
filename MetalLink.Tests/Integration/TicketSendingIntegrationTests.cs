using Xunit;
using MetalLink.Application.Services;

namespace MetalLink.Tests.Integration;

public class TicketSendingIntegrationTests
{
    [Fact]
    public void SendingTicket_WithValidWeighbridgeData_CalculatesCorrectNetWeight()
    {
        // Arrange - For sending: second (loaded) should be less than first (empty)
        var ticketTypeId = 1; // Weighbridge
        var firstWeight = 2000m;  // Empty weight
        var secondWeight = 1000m; // Loaded weight

        // Act
        var validation = WeightCalculationService.ValidateTicketWeights(
            ticketTypeId: ticketTypeId,
            firstWeightKg: firstWeight,
            secondWeightKg: secondWeight,
            isReceiving: false
        );

        var netWeight = WeightCalculationService.CalculateNetWeightFromScale(
            firstWeight,
            secondWeight,
            isReceiving: false
        );

        // Assert
        Assert.True(validation.IsValid);
        Assert.Equal(-1000m, netWeight); // second - first = 1000 - 2000
    }

    [Fact]
    public void SendingTicket_WithInvalidWeights_FailsValidation()
    {
        // Arrange - For sending, second should be LESS than first
        var ticketTypeId = 1; // Weighbridge
        var firstWeight = 1000m;  // Empty weight - but THIS is less than second
        var secondWeight = 2000m; // Loaded weight - INVALID

        // Act
        var validation = WeightCalculationService.ValidateTicketWeights(
            ticketTypeId: ticketTypeId,
            firstWeightKg: firstWeight,
            secondWeightKg: secondWeight,
            isReceiving: false
        );

        // Assert
        Assert.False(validation.IsValid);
        Assert.Contains("second_weight_kg should be less than first_weight_kg", validation.ErrorMessage);
    }

    [Fact]
    public void SendingPlatformTicket_WithoutWeights_PassesValidation()
    {
        // Arrange
        var ticketTypeId = 2; // Platform

        // Act
        var validation = WeightCalculationService.ValidateTicketWeights(
            ticketTypeId: ticketTypeId,
            firstWeightKg: null,
            secondWeightKg: null,
            isReceiving: false
        );

        // Assert
        Assert.True(validation.IsValid);
    }

    [Fact]
    public void SendingTicketLineItems_WithMultiplePrices_CalculatesTotalCorrectly()
    {
        // Arrange
        var lineItems = new List<(decimal NetWeightKg, decimal UnitPricePerKg)>
        {
            (500m, 50m),   // 25000
            (300m, 55m),   // 16500
            (200m, 60m)    // 12000
        };

        // Act
        var (totalExVat, vat, totalIncVat) = TicketCalculationService.CalculateAllTotals(lineItems);

        // Assert
        Assert.Equal(53500m, totalExVat);
        Assert.Equal(8025m, vat);
        Assert.Equal(61525m, totalIncVat);
    }

    [Fact]
    public void SendingTicket_FinancialCalculations_RespectVatRate()
    {
        // Arrange - Test that VAT is exactly 15%
        var totalExVat = 1000m;

        // Act
        var vat = TicketCalculationService.CalculateVat(totalExVat);
        var totalIncVat = TicketCalculationService.CalculateTotalIncVat(totalExVat);

        // Assert
        Assert.Equal(150m, vat);            // 15% of 1000
        Assert.Equal(1150m, totalIncVat);   // 1000 + 150
    }

    [Fact]
    public void SiteCodeGeneration_WithExistingSites_GeneratesCorrectSequence()
    {
        // Arrange
        var existingSites = new List<MetalLink.Domain.Entities.Site>
        {
            new MetalLink.Domain.Entities.Site { SiteId = 1, SiteCode = "SITE-1", SiteName = "Site 1", CompanyId = 1, IsActive = true },
            new MetalLink.Domain.Entities.Site { SiteId = 2, SiteCode = "SITE-2", SiteName = "Site 2", CompanyId = 1, IsActive = true },
            new MetalLink.Domain.Entities.Site { SiteId = 3, SiteCode = "SITE-5", SiteName = "Site 5", CompanyId = 1, IsActive = true }
        };

        // Act
        var nextCode = SiteCodeGeneratorService.GenerateNextSiteCode(existingSites);

        // Assert
        Assert.Equal("SITE-6", nextCode);
    }

    [Fact]
    public void PriceCodeValidation_WithAllValidCodes_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(PriceLookupService.IsValidPriceCode("A"));
        Assert.True(PriceLookupService.IsValidPriceCode("B"));
        Assert.True(PriceLookupService.IsValidPriceCode("C"));
        Assert.True(PriceLookupService.IsValidPriceCode("a")); // lowercase
        Assert.True(PriceLookupService.IsValidPriceCode("b")); // lowercase
        Assert.True(PriceLookupService.IsValidPriceCode("c")); // lowercase
    }

    [Fact]
    public void PriceCodeValidation_WithInvalidCodes_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(PriceLookupService.IsValidPriceCode("D"));
        Assert.False(PriceLookupService.IsValidPriceCode(""));
        Assert.False(PriceLookupService.IsValidPriceCode(null));
        Assert.False(PriceLookupService.IsValidPriceCode("AB"));
    }
}
