using System.Net.Http.Headers;

public interface IAuthorizationService
{
    void AddAuthorizationHeader(HttpRequestMessage request);
}

public class AuthorizationService(ITokenService tokenService) : IAuthorizationService
{
    private readonly ITokenService tokenService = tokenService;

    public void AddAuthorizationHeader(HttpRequestMessage request)
    {
        var accessToken = tokenService.GetAccessToken();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}


public class AuthorizationHandler(ITokenService tokenService) : DelegatingHandler
{
    private readonly ITokenService tokenService = tokenService;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = tokenService.GetAccessToken();
        
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
