namespace MetalLink.Domain.Entities;

public class Customer
{
    public long CustomerId { get; private set; }
    public long SiteId { get; private set; }

    public string FullName { get; private set; } = string.Empty;
    public bool IsCompany { get; private set; }

    public string? CompanyName { get; private set; }
    public string? IdNumber { get; private set; }
    public string? AccountNumber { get; private set; }
    public string? PriceCode { get; private set; }

    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? Suburb { get; private set; }
    public string? City { get; private set; }
    public string? PostalCode { get; private set; }

    public string? PhoneNumber { get; private set; }
    public string? MobileNumber { get; private set; }
    public string? Email { get; private set; }

    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedTime { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedTime { get; private set; } = DateTimeOffset.UtcNow;

    // EF Core needs a parameterless constructor
    private Customer() { }

    public Customer(long siteId, string fullName, bool isCompany = false, string? companyName = null)
    {
        SiteId = siteId;
        FullName = fullName;
        IsCompany = isCompany;
        CompanyName = companyName;
    }

    public void SetContact(
        string? phoneNumber,
        string? mobileNumber,
        string? email)
    {
        PhoneNumber = phoneNumber;
        MobileNumber = mobileNumber;
        Email = email;
        Touch();
    }

    public void SetAddress(
        string? addressLine1,
        string? addressLine2,
        string? suburb,
        string? city,
        string? postalCode)
    {
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        Suburb = suburb;
        City = city;
        PostalCode = postalCode;
        Touch();
    }

    public void SetIdentity(
        string? idNumber,
        string? accountNumber,
        string? priceCode)
    {
        IdNumber = idNumber;
        AccountNumber = accountNumber;
        PriceCode = priceCode;
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
