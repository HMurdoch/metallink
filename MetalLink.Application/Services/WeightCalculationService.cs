namespace MetalLink.Application.Services;

/// <summary>
/// Service for calculating and validating weights in tickets
/// Handles both weighbridge (type 1) and platform (type 2) tickets
/// </summary>
public class WeightCalculationService
{
    /// <summary>
    /// Ticket types
    /// </summary>
    public const int TicketTypeWeighbridge = 1;
    public const int TicketTypePlatform = 2;

    /// <summary>
    /// Calculates net weight for a weighbridge ticket
    /// For Receiving: net_weight = first_weight - second_weight
    /// For Sending: net_weight = second_weight - first_weight
    /// </summary>
    /// <param name="firstWeightKg">First weight reading (tare/empty)</param>
    /// <param name="secondWeightKg">Second weight reading (gross/loaded)</param>
    /// <param name="isReceiving">True for receiving, false for sending</param>
    /// <returns>Calculated net weight</returns>
    public static decimal CalculateNetWeightFromScale(decimal firstWeightKg, decimal secondWeightKg, bool isReceiving)
    {
        if (isReceiving)
            return firstWeightKg - secondWeightKg;
        else
            return secondWeightKg - firstWeightKg;
    }

    /// <summary>
    /// Calculates total net weight for a ticket from all line items
    /// For Weighbridge: Used as validation/backup
    /// For Platform: This is the ONLY source of weight
    /// </summary>
    /// <param name="lineItemWeights">Net weights from all line items</param>
    /// <returns>Sum of all line item weights</returns>
    public static decimal CalculateNetWeightFromLineItems(IEnumerable<decimal> lineItemWeights)
    {
        return lineItemWeights.Sum();
    }

    /// <summary>
    /// Validates that weights are consistent and reasonable for weighbridge tickets
    /// For Receiving: first_weight should be less than second_weight (tare < gross)
    /// For Sending: second_weight should be less than first_weight (gross < empty)
    /// </summary>
    public static bool IsValidWeightPair(decimal firstWeightKg, decimal secondWeightKg, bool isReceiving)
    {
        // Both weights must be positive
        if (firstWeightKg <= 0 || secondWeightKg <= 0)
            return false;

        // For receiving: first (tare) should be less than second (gross)
        if (isReceiving && firstWeightKg >= secondWeightKg)
            return false;

        // For sending: second (gross) should be less than first (empty)
        if (!isReceiving && secondWeightKg >= firstWeightKg)
            return false;

        return true;
    }

    /// <summary>
    /// Validates that a single weight value is reasonable
    /// </summary>
    public static bool IsValidWeight(decimal weightKg)
    {
        return weightKg > 0;
    }

    /// <summary>
    /// Determines if a ticket is weighbridge type
    /// </summary>
    public static bool IsWeighbridgeTicket(int ticketTypeId)
    {
        return ticketTypeId == TicketTypeWeighbridge;
    }

    /// <summary>
    /// Determines if a ticket is platform type
    /// </summary>
    public static bool IsPlatformTicket(int ticketTypeId)
    {
        return ticketTypeId == TicketTypePlatform;
    }

    /// <summary>
    /// Gets validation result for a ticket's weights
    /// </summary>
    public static WeightValidationResult ValidateTicketWeights(
        int ticketTypeId, 
        decimal? firstWeightKg, 
        decimal? secondWeightKg, 
        bool isReceiving)
    {
        // Platform tickets should not have weights
        if (IsPlatformTicket(ticketTypeId))
        {
            if (firstWeightKg.HasValue || secondWeightKg.HasValue)
                return new WeightValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Platform tickets should not have first_weight_kg or second_weight_kg"
                };

            return new WeightValidationResult { IsValid = true };
        }

        // Weighbridge tickets must have both weights
        if (!firstWeightKg.HasValue || !secondWeightKg.HasValue)
            return new WeightValidationResult
            {
                IsValid = false,
                ErrorMessage = "Weighbridge tickets require both first_weight_kg and second_weight_kg"
            };

        // Validate weight pair
        if (!IsValidWeightPair(firstWeightKg.Value, secondWeightKg.Value, isReceiving))
        {
            var message = isReceiving
                ? "For receiving tickets: first_weight_kg should be less than second_weight_kg"
                : "For sending tickets: second_weight_kg should be less than first_weight_kg";

            return new WeightValidationResult
            {
                IsValid = false,
                ErrorMessage = message
            };
        }

        return new WeightValidationResult { IsValid = true };
    }
}

/// <summary>
/// Result object for weight validation
/// </summary>
public class WeightValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}
