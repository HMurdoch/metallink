namespace MetalLink.Application.Services;

/// <summary>
/// Service for calculating ticket totals, VAT, and other financial metrics
/// </summary>
public class TicketCalculationService
{
    private const decimal VatRate = 0.15m; // 15% VAT

    /// <summary>
    /// Calculates total (excluding VAT) from line items
    /// Total (ex. VAT) = SUM(net_weight_kg × unit_price_per_kg) for all line items
    /// </summary>
    /// <param name="lineItems">List of (netWeightKg, unitPricePerKg) tuples</param>
    /// <returns>Total excluding VAT</returns>
    public static decimal CalculateTotalExVat(IEnumerable<(decimal NetWeightKg, decimal UnitPricePerKg)> lineItems)
    {
        return lineItems.Sum(item => item.NetWeightKg * item.UnitPricePerKg);
    }

    /// <summary>
    /// Calculates VAT at 15% of total (ex. VAT)
    /// </summary>
    /// <param name="totalExVat">Total excluding VAT</param>
    /// <returns>VAT amount (15% of total)</returns>
    public static decimal CalculateVat(decimal totalExVat)
    {
        return totalExVat * VatRate;
    }

    /// <summary>
    /// Calculates total including VAT
    /// Total (incl. VAT) = Total (ex. VAT) + VAT
    /// </summary>
    /// <param name="totalExVat">Total excluding VAT</param>
    /// <returns>Total including VAT</returns>
    public static decimal CalculateTotalIncVat(decimal totalExVat)
    {
        var vat = CalculateVat(totalExVat);
        return totalExVat + vat;
    }

    /// <summary>
    /// Calculates all three financial values at once
    /// </summary>
    /// <param name="lineItems">List of (netWeightKg, unitPricePerKg) tuples</param>
    /// <returns>Tuple of (TotalExVat, Vat, TotalIncVat)</returns>
    public static (decimal TotalExVat, decimal Vat, decimal TotalIncVat) CalculateAllTotals(IEnumerable<(decimal NetWeightKg, decimal UnitPricePerKg)> lineItems)
    {
        var totalExVat = CalculateTotalExVat(lineItems);
        var vat = CalculateVat(totalExVat);
        var totalIncVat = totalExVat + vat;

        return (totalExVat, vat, totalIncVat);
    }

    /// <summary>
    /// Calculates net weight from first and second weight for weighbridge tickets
    /// For Receiving: net_weight = first_weight - second_weight
    /// For Sending: net_weight = second_weight - first_weight
    /// </summary>
    /// <param name="firstWeightKg">The first weight reading (tare weight)</param>
    /// <param name="secondWeightKg">The second weight reading (gross weight)</param>
    /// <param name="isReceiving">True if receiving ticket, false if sending</param>
    /// <returns>Net weight in kg</returns>
    public static decimal CalculateNetWeightFromScale(decimal firstWeightKg, decimal secondWeightKg, bool isReceiving)
    {
        if (isReceiving)
            return firstWeightKg - secondWeightKg;
        else
            return secondWeightKg - firstWeightKg;
    }

    /// <summary>
    /// Validates that weight values are reasonable (positive and not zero)
    /// </summary>
    public static bool IsValidWeightKg(decimal weightKg)
    {
        return weightKg > 0;
    }

    /// <summary>
    /// Validates that price is reasonable (positive)
    /// </summary>
    public static bool IsValidPrice(decimal pricePerKg)
    {
        return pricePerKg >= 0;
    }
}
