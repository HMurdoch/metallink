using Xunit;
using MetalLink.Application.Services;
using MetalLink.Domain.Entities;

namespace MetalLink.Tests.Services;

public class CompanyValidationServiceTests
{
    [Fact]
    public void ValidateCompanyAndSite_WithNonCompany_ReturnsValid()
    {
        // Act
        var result = CompanyValidationService.ValidateCompanyAndSite(isCompany: false, companyId: null, siteId: null);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateCompanyAndSite_WithCompanyButNoCompanyId_ReturnsInvalid()
    {
        // Act
        var result = CompanyValidationService.ValidateCompanyAndSite(isCompany: true, companyId: null, siteId: 1);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("company_id is required", result.ErrorMessage);
    }

    [Fact]
    public void ValidateCompanyAndSite_WithCompanyButNoSiteId_ReturnsInvalid()
    {
        // Act
        var result = CompanyValidationService.ValidateCompanyAndSite(isCompany: true, companyId: 1, siteId: null);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("site_id is required", result.ErrorMessage);
    }

    [Fact]
    public void ValidateCompanyAndSite_WithCompanyAndBothIds_ReturnsValid()
    {
        // Act
        var result = CompanyValidationService.ValidateCompanyAndSite(isCompany: true, companyId: 1, siteId: 1);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateCompanyAndSite_WithInvalidCompanyId_ReturnsInvalid()
    {
        // Act
        var result = CompanyValidationService.ValidateCompanyAndSite(isCompany: true, companyId: 0, siteId: 1);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("company_id is required", result.ErrorMessage);
    }

    [Fact]
    public void ValidateCompanyAndSite_WithInvalidSiteId_ReturnsInvalid()
    {
        // Act
        var result = CompanyValidationService.ValidateCompanyAndSite(isCompany: true, companyId: 1, siteId: 0);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("site_id is required", result.ErrorMessage);
    }

    [Fact]
    public void SiteBelongsToCompany_WithValidSite_ReturnsTrue()
    {
        // Arrange
        var sites = new List<Site>
        {
            new Site { SiteId = 1, SiteName = "Site 1", CompanyId = 1, IsActive = true },
            new Site { SiteId = 2, SiteName = "Site 2", CompanyId = 1, IsActive = true }
        };

        // Act
        var belongs = CompanyValidationService.SiteBelongsToCompany(companyId: 1, siteId: 1, sites: sites);

        // Assert
        Assert.True(belongs);
    }

    [Fact]
    public void SiteBelongsToCompany_WithInactiveSite_ReturnsFalse()
    {
        // Arrange
        var sites = new List<Site>
        {
            new Site { SiteId = 1, SiteName = "Site 1", CompanyId = 1, IsActive = false }
        };

        // Act
        var belongs = CompanyValidationService.SiteBelongsToCompany(companyId: 1, siteId: 1, sites: sites);

        // Assert
        Assert.False(belongs);
    }

    [Fact]
    public void SiteBelongsToCompany_WithNonExistentSite_ReturnsFalse()
    {
        // Arrange
        var sites = new List<Site>
        {
            new Site { SiteId = 1, SiteName = "Site 1", CompanyId = 1, IsActive = true }
        };

        // Act
        var belongs = CompanyValidationService.SiteBelongsToCompany(companyId: 1, siteId: 999, sites: sites);

        // Assert
        Assert.False(belongs);
    }

    [Fact]
    public void SiteBelongsToCompany_WithNullSites_ReturnsFalse()
    {
        // Act
        var belongs = CompanyValidationService.SiteBelongsToCompany(companyId: 1, siteId: 1, sites: null);

        // Assert
        Assert.False(belongs);
    }

    [Fact]
    public void SiteBelongsToCompany_WithEmptySiteList_ReturnsFalse()
    {
        // Act
        var belongs = CompanyValidationService.SiteBelongsToCompany(companyId: 1, siteId: 1, sites: new List<Site>());

        // Assert
        Assert.False(belongs);
    }
}
