using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using Serilog;
using Microsoft.IdentityModel.Protocols;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSerilog();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddHttpClient("LoyaltyService", client =>
{
    client.BaseAddress = new Uri("http://loyalty-service.default.svc.cluster.local:8050");
});

builder.Services.AddHttpClient("PaymentService", client =>
{
    client.BaseAddress = new Uri("http://payment-service.default.svc.cluster.local:8060");
});

builder.Services.AddHttpClient("ReservationService", client =>
{
    client.BaseAddress = new Uri("http://reservation-service.default.svc.cluster.local:8070");
});


builder.Services.AddMvc();

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = "https://dev-qsbo6smqgu2rkhti.us.auth0.com/";
    options.Audience = "https://dips5.com/api";

    options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = "https://dev-qsbo6smqgu2rkhti.us.auth0.com/",
    ValidAudience = "https://dips5.com/api"
};

options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
    "https://dev-qsbo6smqgu2rkhti.us.auth0.com/.well-known/openid-configuration",
    new OpenIdConnectConfigurationRetriever(),
    new HttpDocumentRetriever());

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



var app = builder.Build();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // Маппинг контроллеров через Endpoint Routing

app.UseStaticFiles();

// Использование Endpoint Routing
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();  // Маппинг контроллеров через Endpoint Routing
});

app.Run();
