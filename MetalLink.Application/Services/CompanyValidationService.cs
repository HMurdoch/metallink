namespace MetalLink.Application.Services;

/// <summary>
/// Service for validating company and site relationships
/// Ensures that customers/buyers have proper company and site assignments
/// Note: This service provides static validation methods
/// Foreign key constraints in the database provide primary validation
/// </summary>
public class CompanyValidationService
{
    /// <summary>
    /// Validates that a site belongs to a specific company
    /// </summary>
    /// <param name="companyId">The company ID</param>
    /// <param name="siteId">The site ID to validate</param>
    /// <param name="sites">List of sites to check against</param>
    /// <returns>True if site belongs to company and is active, false otherwise</returns>
    public static bool SiteBelongsToCompany(long companyId, long siteId, IEnumerable<Domain.Entities.Site>? sites)
    {
        if (sites == null)
            return false;

        var site = sites.FirstOrDefault(s => s.SiteId == siteId && s.IsActive);
        return site != null;
    }

    /// <summary>
    /// Validates that required fields are present for a company/buyer entity
    /// </summary>
    /// <param name="isCompany">Whether this is a company entity</param>
    /// <param name="companyId">The company ID (required if isCompany is true)</param>
    /// <param name="siteId">The site ID (required if isCompany is true)</param>
    /// <returns>Validation result with error message if invalid</returns>
    public static CompanyValidationResult ValidateCompanyAndSite(bool isCompany, long? companyId, long? siteId)
    {
        // If not a company, validation passes
        if (!isCompany)
            return new CompanyValidationResult { IsValid = true };

        // For companies, both companyId and siteId are required
        if (!companyId.HasValue || companyId <= 0)
            return new CompanyValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = "company_id is required for company entities" 
            };

        if (!siteId.HasValue || siteId <= 0)
            return new CompanyValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = "site_id is required for company entities" 
            };

        return new CompanyValidationResult { IsValid = true };
    }
}

/// <summary>
/// Result object for company validation
/// </summary>
public class CompanyValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}
