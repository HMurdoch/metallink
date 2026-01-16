using MetalLink.Application.Interfaces;

namespace MetalLink.Application.Services;

/// <summary>
/// Service for generating ticket numbers based on ticket type
/// Generates WB-00000001, WB-00000002 for weighbridge tickets
/// Generates PL-00000001, PL-00000002 for platform tickets
/// </summary>
public class TicketNumberService
{
    private readonly ITicketReceivingRepository _ticketReceivingRepo;

    public TicketNumberService(ITicketReceivingRepository ticketReceivingRepo)
    {
        _ticketReceivingRepo = ticketReceivingRepo;
    }

    /// <summary>
    /// Gets the next ticket number based on ticket type
    /// </summary>
    /// <param name="ticketTypeId">1 = Weighbridge (WB), 2 = Platform (PL)</param>
    /// <returns>Next ticket number (e.g., WB-00000001)</returns>
    public async Task<string> GetNextTicketNumberAsync(int ticketTypeId)
    {
        var prefix = GetPrefix(ticketTypeId);
        var lastNumber = await GetLastTicketNumberAsync(ticketTypeId);
        var nextNumber = lastNumber + 1;

        return $"{prefix}-{nextNumber:D8}";
    }

    /// <summary>
    /// Gets the prefix based on ticket type
    /// </summary>
    private static string GetPrefix(int ticketTypeId)
    {
        return ticketTypeId switch
        {
            1 => "WB", // Weighbridge
            2 => "PL", // Platform
            _ => throw new ArgumentException($"Invalid ticket type id: {ticketTypeId}")
        };
    }

    /// <summary>
    /// Gets the last sequential number for a given ticket type
    /// </summary>
    private async Task<int> GetLastTicketNumberAsync(int ticketTypeId)
    {
        var prefix = GetPrefix(ticketTypeId);
        
        // This would need to be implemented in the repository
        // For now, we'll provide a basic implementation
        // You may want to use a database query or stored procedure for better performance
        
        var lastTicketNumber = await _ticketReceivingRepo.GetLastTicketNumberByPrefixAsync(prefix);
        
        if (string.IsNullOrEmpty(lastTicketNumber))
            return 0;

        // Extract the numeric part (e.g., "WB-00000005" -> 5)
        var numericPart = lastTicketNumber.Substring(3); // Skip "WB-" or "PL-"
        if (int.TryParse(numericPart, out var number))
            return number;

        return 0;
    }
}
