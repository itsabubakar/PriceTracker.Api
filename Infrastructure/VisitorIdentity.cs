using System.Security.Claims;

namespace PriceTracker.Api.Infrastructure;

public static class VisitorIdentity
{
    public const string AnonymousCookieName = "pt_anon_id";

    public static string BuildOwnerKey(HttpContext httpContext)
    {
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var sub = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirstValue("sub");

            if (!string.IsNullOrWhiteSpace(sub))
                return $"user:{sub}";
        }

        if (httpContext.Request.Cookies.TryGetValue(AnonymousCookieName, out var anonId) &&
            Guid.TryParse(anonId, out _))
        {
            return $"anon:{anonId}";
        }

        throw new InvalidOperationException("Visitor key is missing.");
    }
}
