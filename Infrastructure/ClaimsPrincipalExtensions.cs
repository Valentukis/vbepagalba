using System.Security.Claims;

namespace VbeSort.Infrastructure;

public static class AppClaimTypes
{
    public const string DisplayName = "vbe:display_name";
}

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static string GetDisplayName(this ClaimsPrincipal principal)
    {
        var display = principal.FindFirstValue(AppClaimTypes.DisplayName);
        return string.IsNullOrWhiteSpace(display) ? principal.Identity?.Name ?? "" : display;
    }
}
