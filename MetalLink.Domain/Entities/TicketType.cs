namespace MetalLink.Domain.Entities;

public class TicketType
{
    public int TicketTypeId { get; private set; }
    public string TicketTypeName { get; private set; } = string.Empty;
    public int CreatedByOperatorId { get; private set; } = 1;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; private set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public ICollection<TicketReceiving> TicketsReceiving { get; set; } = new List<TicketReceiving>();

    private TicketType() { }

    public TicketType(string ticketTypeName, bool isActive = true)
    {
        TicketTypeName = ticketTypeName;
        IsActive = isActive;
    }

    public void Update(string ticketTypeName, bool isActive)
    {
        TicketTypeName = ticketTypeName;
        IsActive = isActive;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedTime = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedTime = DateTimeOffset.UtcNow;
    }
}
