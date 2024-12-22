using GatewayService.ITokenService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<AuthorizationHandler>();

builder.Services.AddHttpClient("LoyaltyService", client =>
{
    client.BaseAddress = new Uri("http://loyalty-service.default.svc.cluster.local:8050");
    // client.BaseAddress = new Uri("http://loyalty_service:8050");
}).AddHttpMessageHandler<AuthorizationHandler>(); ;

builder.Services.AddHttpClient("PaymentService", client =>
{
    client.BaseAddress = new Uri("http://payment-service.default.svc.cluster.local:8060");
    // client.BaseAddress = new Uri("http://payment_service:8060");
}).AddHttpMessageHandler<AuthorizationHandler>(); ;

builder.Services.AddHttpClient("ReservationService", client =>
{
    client.BaseAddress = new Uri("http://reservation-service.default.svc.cluster.local:8070");
    // client.BaseAddress = new Uri("http://reservation_service:8070");
}).AddHttpMessageHandler<AuthorizationHandler>(); ;

builder.Services.AddAutoMapper(typeof(Program));

var domain = $"https://{builder.Configuration["Auth:Domain"]}/";
var apiIdentifier = builder.Configuration["Auth:Api"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = domain;
        options.Audience = apiIdentifier;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = domain,
            ValidateAudience = true,
            ValidAudience = apiIdentifier,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"https://dev-qsbo6smqgu2rkhti.us.auth0.com/.well-known/openid-configuration",
                    new OpenIdConnectConfigurationRetriever());
                var config = configManager.GetConfigurationAsync(CancellationToken.None).Result;
                return config.JsonWebKeySet.GetSigningKeys();
            }
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("Authentication failed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Authentication challenge triggered: {Error}, {ErrorDescription}", context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token validated successfully.");
                return Task.CompletedTask;
            }
        };
    });

builder.Logging
    .SetMinimumLevel(LogLevel.Information)
    .AddConsole();

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
