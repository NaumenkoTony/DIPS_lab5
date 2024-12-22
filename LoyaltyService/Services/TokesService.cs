namespace LoyaltyService.ITokenService;

using System.IdentityModel.Tokens.Jwt;
using Serilog;

public interface ITokenService
{
    string GetAccessToken();
    string GetUsernameFromJWT();
}

public class TokenService(IHttpContextAccessor httpContextAccessor) : ITokenService
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

    public string GetAccessToken()
    {
        var authorizationHeader = httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(authorizationHeader))
        {
            throw new UnauthorizedAccessException("Authorization header is missing.");
        }
        var token = authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader.Substring("Bearer ".Length).Trim()
            : null;

        if (string.IsNullOrEmpty(token))
        {
            throw new UnauthorizedAccessException("Token is missing or invalid in the Authorization header.");
        }

        return token;
    }

    public string GetUsernameFromJWT()
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();

            var jwtToken = handler.ReadJwtToken(GetAccessToken());
            
            var usernameClaim = jwtToken?.Claims.FirstOrDefault(c => c.Type == "nickname")?.Value;

            return usernameClaim;
        }
        catch (Exception ex)
        {
            Log.Error($"Error extracting username from token: {ex.Message}");
            throw new UnauthorizedAccessException("Invalid token or unable to extract username.", ex);
        }
    }
}
