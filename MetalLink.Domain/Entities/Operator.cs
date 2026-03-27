namespace MetalLink.Domain.Entities;

public class Operator
{
    public int OperatorId { get; private set; }

    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;

    // Hashed password (never store plain text)
    public string PasswordHash { get; private set; } = string.Empty;

    // Simple role string for now: "Admin", "Operator", "Supervisor"
    public string Role { get; private set; } = "Operator";

    public int CreatedByOperatorId { get; private set; } = 1;

    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; private set; } = DateTimeOffset.UtcNow;

    public virtual ICollection<OperatorSetting> OperatorSettings { get; set; } = new List<OperatorSetting>();

    private Operator() { }

    public Operator(string username, string displayName, string passwordHash, string role = "Operator")
    {
        Username = username;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        Role = role;
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        Touch();
    }

    public void SetRole(string role)
    {
        Role = role;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    private void Touch()
    {
        UpdatedTime = DateTimeOffset.UtcNow;
    }
}
