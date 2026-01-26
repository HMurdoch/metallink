using System.Security.Claims;

namespace MetalLink.Api.Extensions;

/// <summary>
/// Extension methods for extracting information from JWT tokens/claims
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Gets the operator ID from the authenticated user's claims
    /// The operator ID is stored in the "sub" (subject) claim
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from HttpContext.User</param>
    /// <returns>The operator ID, or 1 (default) if not found</returns>
    public static long GetOperatorId(this ClaimsPrincipal user)
    {
        if (user == null)
            return 1; // Default to operator 1 (system)

        // JWT tokens use "sub" (subject) claim for the user ID
        var subClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        
        if (subClaim != null && long.TryParse(subClaim.Value, out var operatorId))
            return operatorId;

        // Fallback: try to get from claim directly
        var operatorClaim = user.FindFirst("operatorId");
        if (operatorClaim != null && long.TryParse(operatorClaim.Value, out var id))
            return id;

        return 1; // Default fallback
    }

    /// <summary>
    /// Gets the display name of the authenticated user
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from HttpContext.User</param>
    /// <returns>The display name or username</returns>
    public static string GetDisplayName(this ClaimsPrincipal user)
    {
        if (user == null)
            return "System";

        var displayNameClaim = user.FindFirst("display_name") ?? user.FindFirst("name");
        if (displayNameClaim != null)
            return displayNameClaim.Value;

        var usernameClaim = user.FindFirst(ClaimTypes.Name);
        if (usernameClaim != null)
            return usernameClaim.Value;

        return "Unknown";
    }

    /// <summary>
    /// Gets the username of the authenticated user
    /// </summary>
    /// <param name="user">The ClaimsPrincipal from HttpContext.User</param>
    /// <returns>The username</returns>
    public static string GetUsername(this ClaimsPrincipal user)
    {
        if (user == null)
            return "system";

        var usernameClaim = user.FindFirst(ClaimTypes.Name) ?? user.FindFirst("username");
        return usernameClaim?.Value ?? "unknown";
    }

    /// <summary>
    /// Checks if the user is authenticated
    /// </summary>
    public static bool IsAuthenticated(this ClaimsPrincipal user)
    {
        return user?.Identity?.IsAuthenticated ?? false;
    }
}
