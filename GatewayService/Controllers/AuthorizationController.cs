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

[Route("/api/v1/authorize")]
[ApiController]
public class AuthorizationController : ControllerBase
{
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(ILogger<AuthorizationController> logger)
    {
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest("Username and password are required.");

        var domain = "https://dev-qsbo6smqgu2rkhti.us.auth0.com/";
        var apiIdentifier = "https://dips5.com/api";
        var clientId = "GGwBXsoEGqhRAEYOc97qW6lU5yu2ojwN";
        var clientSecret = "JDgvvoMQxxC7IWdpkBP8a4MkQE1KxjNTZQ0o2_8avjbfj7zIcGRyMGBReydOCZx3";

        var client = new HttpClient();
        var response = await client.PostAsync($"{domain}oauth/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "scope", "openid" },
                { "grant_type", "password" },
                { "username", request.Username },
                { "password", request.Password },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "audience",  apiIdentifier }
            }));

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Login failed for user {Username}. Response: {StatusCode} - {ReasonPhrase}",
                request.Username, response.StatusCode, response.ReasonPhrase);
            return Unauthorized("Invalid username or password.");
        }

        var tokenResponse = await response.Content.ReadAsStringAsync();
        return Ok(tokenResponse);
    }

    [AllowAnonymous]
    [HttpPost("callback")]
    public IActionResult Callback()
    {
        _logger.LogInformation("Callback received.");
        return Ok("Callback received.");
    }
}
