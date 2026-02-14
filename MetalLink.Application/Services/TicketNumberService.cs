using MetalLink.Application.Interfaces;

namespace MetalLink.Application.Services;

/// <summary>
/// Service for generating ticket numbers based on ticket type and direction
/// Receiving Weighbridge: RWB-00000001
/// Receiving Platform: RPL-00000001
/// Sending Weighbridge: SWB-00000001
/// Sending Platform: SPL-00000001
/// </summary>
public class TicketNumberService
{
    private readonly ITicketReceivingRepository _ticketReceivingRepo;
    private readonly ITicketSendingRepository _ticketSendingRepo;

    public TicketNumberService(ITicketReceivingRepository ticketReceivingRepo, ITicketSendingRepository ticketSendingRepo)
    {
        _ticketReceivingRepo = ticketReceivingRepo;
        _ticketSendingRepo = ticketSendingRepo;
    }

    /// <summary>
    /// Gets the next receiving ticket number based on ticket type
    /// </summary>
    /// <param name="ticketTypeId">1 = Weighbridge (RWB), 2 = Platform (RPL)</param>
    /// <returns>Next ticket number (e.g., RWB-00000001)</returns>
    public async Task<string> GetNextReceivingTicketNumberAsync(int ticketTypeId)
    {
        var prefix = GetReceivingPrefix(ticketTypeId);
        var seqValue = await _ticketReceivingRepo.GetNextTicketSequenceValueAsync(prefix);
        return $"{prefix}-{seqValue:D8}";
    }

    /// <summary>
    /// Gets the next sending ticket number based on ticket type
    /// </summary>
    /// <param name="ticketTypeId">1 = Weighbridge (SWB), 2 = Platform (SPL)</param>
    /// <returns>Next ticket number (e.g., SWB-00000001)</returns>
    public async Task<string> GetNextSendingTicketNumberAsync(int ticketTypeId)
    {
        var prefix = GetSendingPrefix(ticketTypeId);
        var seqValue = await _ticketSendingRepo.GetNextTicketSequenceValueAsync(prefix);
        return $"{prefix}-{seqValue:D8}";
    }

    /// <summary>
    /// Gets the prefix for receiving tickets based on ticket type
    /// </summary>
    private static string GetReceivingPrefix(int ticketTypeId)
    {
        return ticketTypeId switch
        {
            1 => "RWB", // Receiving Weighbridge
            2 => "RPL", // Receiving Platform
            _ => throw new ArgumentException($"Invalid ticket type id: {ticketTypeId}")
        };
    }

    /// <summary>
    /// Gets the prefix for sending tickets based on ticket type
    /// </summary>
    private static string GetSendingPrefix(int ticketTypeId)
    {
        return ticketTypeId switch
        {
            1 => "SWB", // Sending Weighbridge
            2 => "SPL", // Sending Platform
            _ => throw new ArgumentException($"Invalid ticket type id: {ticketTypeId}")
        };
    }

    /// <summary>
    /// Extracts the numeric portion from a ticket number
    /// </summary>
    private static int ExtractNumberFromTicketNumber(string? ticketNumber, string prefix)
    {
        if (string.IsNullOrEmpty(ticketNumber))
            return 0;

        try
        {
            // Remove prefix and dash (e.g., "RWB-00000005" -> "00000005")
            var numericPart = ticketNumber.Substring(prefix.Length + 1);
            if (int.TryParse(numericPart, out var number))
                return number;
        }
        catch
        {
            return 0;
        }

        return 0;
    }
}
