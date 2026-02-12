namespace MetalLink.Application.Services;

/// <summary>
/// Service for generating site codes following the pattern SITE-N
/// where N is auto-incremented for each new site within a company
/// </summary>
public class SiteCodeGeneratorService
{
    /// <summary>
    /// Generates the next site code for a company
    /// Finds the highest existing SITE-N for the company and increments by 1
    /// </summary>
    /// <param name="existingSites">List of existing sites for the company</param>
    /// <returns>The next site code (e.g., SITE-1, SITE-2, SITE-3)</returns>
    public static string GenerateNextSiteCode(IEnumerable<Domain.Entities.Site>? existingSites)
    {
        if (existingSites == null || !existingSites.Any())
            return "SITE-1";

        // Extract numeric portion from all SITE-N codes
        var siteNumbers = new List<int>();
        foreach (var site in existingSites)
        {
            if (!string.IsNullOrEmpty(site.SiteCode) && site.SiteCode.StartsWith("SITE-"))
            {
                var numericPart = site.SiteCode.Substring(5); // Remove "SITE-"
                if (int.TryParse(numericPart, out var number))
                    siteNumbers.Add(number);
            }
        }

        // Get the highest number and increment
        var nextNumber = siteNumbers.Any() ? siteNumbers.Max() + 1 : 1;
        return $"SITE-{nextNumber}";
    }

    /// <summary>
    /// Validates that a site code follows the correct pattern
    /// </summary>
    public static bool IsValidSiteCode(string? siteCode)
    {
        if (string.IsNullOrWhiteSpace(siteCode))
            return false;

        if (!siteCode.StartsWith("SITE-"))
            return false;

        var numericPart = siteCode.Substring(5);
        return int.TryParse(numericPart, out _);
    }

    /// <summary>
    /// Extracts the numeric portion from a site code
    /// </summary>
    public static int? ExtractSiteNumber(string? siteCode)
    {
        if (!IsValidSiteCode(siteCode))
            return null;

        var numericPart = siteCode!.Substring(5);
        if (int.TryParse(numericPart, out var number))
            return number;

        return null;
    }
}
