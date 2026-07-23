using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Hakeem.Api.Extensions
{
    public static class ClaimsExtension
    {
        public static int GetUserId(this ClaimsPrincipal claims)
        {
            if (claims == null)
            {
                throw new UnauthorizedAccessException("User context is missing.");
            }

            // Prefer the explicit CMS user ID claim injected by CmsClaimsTransformation
            var cmsIdClaim = claims.FindFirst("CmsUserId");
            if (cmsIdClaim != null && int.TryParse(cmsIdClaim.Value, out var cmsId))
            {
                return cmsId;
            }

            // Fall back: scan all NameIdentifier claims for a valid integer
            // (Azure AD sets NameIdentifier to a GUID; CmsHeaderAuth sets it to an int)
            foreach (var c in claims.FindAll(ClaimTypes.NameIdentifier))
            {
                if (int.TryParse(c.Value, out var id))
                    return id;
            }

            throw new UnauthorizedAccessException("User identifier claim is missing or invalid.");
        }

        public static IEnumerable<int> GetRoleIds(this ClaimsPrincipal claims)
        {
            return claims.FindAll("RoleId")
                .Select(c => int.TryParse(c.Value, out int id) ? id : 0)
                .Where(id => id > 0)
                .Distinct();
        }
    }
}