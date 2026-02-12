using Xunit;
using MetalLink.Application.Services;
using MetalLink.Domain.Entities;

namespace MetalLink.Tests.Services;

public class SiteCodeGeneratorServiceTests
{
    [Fact]
    public void GenerateNextSiteCode_WithNoExistingSites_ReturnsFirstCode()
    {
        // Act
        var code = SiteCodeGeneratorService.GenerateNextSiteCode(null);

        // Assert
        Assert.Equal("SITE-1", code);
    }

    [Fact]
    public void GenerateNextSiteCode_WithEmptyList_ReturnsFirstCode()
    {
        // Arrange
        var sites = new List<Site>();

        // Act
        var code = SiteCodeGeneratorService.GenerateNextSiteCode(sites);

        // Assert
        Assert.Equal("SITE-1", code);
    }

    [Fact]
    public void GenerateNextSiteCode_WithExistingSites_ReturnsNextNumber()
    {
        // Arrange
        var sites = new List<Site>
        {
            new Site { SiteId = 1, SiteCode = "SITE-1", SiteName = "Site 1", CompanyId = 1, IsActive = true },
            new Site { SiteId = 2, SiteCode = "SITE-2", SiteName = "Site 2", CompanyId = 1, IsActive = true },
            new Site { SiteId = 3, SiteCode = "SITE-3", SiteName = "Site 3", CompanyId = 1, IsActive = true }
        };

        // Act
        var code = SiteCodeGeneratorService.GenerateNextSiteCode(sites);

        // Assert
        Assert.Equal("SITE-4", code);
    }

    [Fact]
    public void GenerateNextSiteCode_WithNonSequentialNumbers_ReturnsHighestPlus1()
    {
        // Arrange
        var sites = new List<Site>
        {
            new Site { SiteId = 1, SiteCode = "SITE-1", SiteName = "Site 1", CompanyId = 1, IsActive = true },
            new Site { SiteId = 2, SiteCode = "SITE-5", SiteName = "Site 5", CompanyId = 1, IsActive = true },
            new Site { SiteId = 3, SiteCode = "SITE-3", SiteName = "Site 3", CompanyId = 1, IsActive = true }
        };

        // Act
        var code = SiteCodeGeneratorService.GenerateNextSiteCode(sites);

        // Assert
        Assert.Equal("SITE-6", code);
    }

    [Fact]
    public void IsValidSiteCode_WithValidCode_ReturnsTrue()
    {
        Assert.True(SiteCodeGeneratorService.IsValidSiteCode("SITE-1"));
        Assert.True(SiteCodeGeneratorService.IsValidSiteCode("SITE-123"));
    }

    [Fact]
    public void IsValidSiteCode_WithInvalidCode_ReturnsFalse()
    {
        Assert.False(SiteCodeGeneratorService.IsValidSiteCode("SITE-"));
        Assert.False(SiteCodeGeneratorService.IsValidSiteCode("SITE-ABC"));
        Assert.False(SiteCodeGeneratorService.IsValidSiteCode("SITE1"));
        Assert.False(SiteCodeGeneratorService.IsValidSiteCode(""));
        Assert.False(SiteCodeGeneratorService.IsValidSiteCode(null));
    }

    [Fact]
    public void ExtractSiteNumber_WithValidCode_ReturnsNumber()
    {
        // Act
        var number = SiteCodeGeneratorService.ExtractSiteNumber("SITE-5");

        // Assert
        Assert.Equal(5, number);
    }

    [Fact]
    public void ExtractSiteNumber_WithInvalidCode_ReturnsNull()
    {
        Assert.Null(SiteCodeGeneratorService.ExtractSiteNumber("INVALID"));
        Assert.Null(SiteCodeGeneratorService.ExtractSiteNumber(null));
    }
}
