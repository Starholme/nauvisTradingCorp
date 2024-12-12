using System.Security.Claims;

namespace web.Helpers
{
    public static class ClaimsPrincipalExtensions
    {
        public static string FullName(this ClaimsPrincipal claimsPrincipal)
        {
            var givenNameClaim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.GivenName);
            var surNameClaim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Surname);

            if (givenNameClaim == null || surNameClaim == null) return "";

            return string.Concat(givenNameClaim.Value, " ", surNameClaim.Value);
        }

        public static Guid GetUserId(this ClaimsPrincipal claimsPrincipal)
        {
            var userIdClaim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            try
            {
                return Guid.Parse(userIdClaim.Value);
            }
            catch (Exception e)
            {
                throw new UnauthorizedAccessException(e.Message);
            }
        }
    }
}
