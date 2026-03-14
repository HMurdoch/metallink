using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MetalLink.Application.Interfaces;
using MetalLink.Shared.Customers;

namespace MetalLink.Application.Customers.Queries;

public sealed class SearchCustomersQueryHandler
    : IRequestHandler<SearchCustomersQuery, CustomerDto[]>
{
    private readonly ICustomerRepository _customerRepository;

    public SearchCustomersQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerDto[]> Handle(
        SearchCustomersQuery query,
        CancellationToken cancellationToken)
    {
        // Use the DTO that the query already wraps
        var criteria = query.Request ?? new CustomerSearchRequestDto();

        // Repository now has the DTO-based SearchAsync
        var customers = await _customerRepository.SearchAsync(criteria, cancellationToken);

        // Map domain Customer (+ Company + Site + Province + Country) to the shared DTO
        return customers
            .Select(c =>
            {
                var company  = c.Company;
                var site     = c.Site;
                var province = site?.Province;
                var country  = site?.Country;
                var imagePath = c.ImagePath;

                return new CustomerDto
                {
                    CustomerId = c.CustomerId,
                    CompanyId  = c.CompanyId,
                    SiteId     = c.SiteId,

                    FullName  = c.FullName,
                    FirstName = c.FirstName,
                    LastName  = c.LastName,
                    IsCompany = c.IsCompany,

                    CompanyName = company?.CompanyName,
                    VatNumber   = company?.VatNumber,
                    IsTaxable   = c.IsTaxable,
                    Taxable     = c.IsTaxable,

                    SiteName = site?.SiteName ?? string.Empty,
                    SiteCode = site?.SiteCode ?? string.Empty,

                    IdNumber      = c.IdNumber,
                    AccountNumber = c.AccountNumber,
                    ProductPriceListId = c.ProductPriceListId,
                    ProductPriceListName = c.ProductPriceList?.ProductPriceListName,

                    PhoneNumber   = c.PhoneNumber,
                    MobileNumber  = c.MobileNumber,
                    Email         = c.Email,

                    IsActive    = c.IsActive,
                    CreatedTime = c.CreatedTime,
                    UpdatedTime = c.UpdatedTime,

                    ImagePathId = c.ImagePathId,
                    IdCardImagePath = imagePath?.IdCardImagePath,
                    DriverLicenseImagePath = imagePath?.DriverLicenseImagePath,
                    PhotoImagePath = imagePath?.PhotoImagePath,
                    SignatureImagePath = imagePath?.SignatureImagePath,
                    FingerprintImagePath = imagePath?.FingerprintImagePath
                };
            })
            .ToArray();
    }
}
