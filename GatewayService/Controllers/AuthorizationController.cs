namespace GatewayService.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using GatewayService.Models.Dto;
using Newtonsoft.Json;
using AutoMapper;
using System.Text;
using GatewayService.Models.ReservationServiceDto;
using GatewayService.Models.LoyaltyServiceDto;
using GatewayService.Models.PaymentServiceDto;
using Microsoft.AspNetCore.Authorization;

public class AuthorizationController(IHttpClientFactory httpClientFactory, IMapper mapper, ILogger<AuthorizationController> logger, IConfiguration configuration) : ControllerBase
{
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly IMapper mapper = mapper;
    private readonly ILogger<AuthorizationController> logger = logger;
    private readonly IConfiguration configuration = configuration;

    [Route("/api/v1/authorize")]
    [HttpPost]
    public async Task<IActionResult> Authorize([FromBody] LoginRequest request)
    {
        var client = new HttpClient();
        var response = await client.PostAsync($"https://{configuration.GetSection("Auth0:Domain")}/oauth/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
            { "grant_type", "password" },
            { "client_id", configuration.GetSection("Auth0:Client_id").ToString()!},
            { "client_secret", configuration.GetSection("Auth0:Client_secret").ToString()!},
            { "username", request.Username },
            { "password", request.Password },
            { "scope", "openid profile email" }
            }));

        if (!response.IsSuccessStatusCode)
            return Unauthorized();

        var tokenResponse = await response.Content.ReadAsStringAsync();
        return Ok(tokenResponse);
    }

    [AllowAnonymous]
    [HttpPost("/api/v1/callback")]
    public IActionResult Callback()
    {
        return Ok("Callback received");
    }
}