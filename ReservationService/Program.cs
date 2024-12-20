using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Data.RepositoriesPostgreSQL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSerilog();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);


builder.Services.AddDbContext<ReservationsContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ReservationService")));

builder.Services.AddScoped<IHotelRepository, HotelRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddMvc();

builder.Services.AddAutoMapper(typeof(Program));


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
        }
    };
});



var app = builder.Build();

app.UseRouting();  // Убедитесь, что используете Endpoint Routing

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();  // Это будет обрабатывать все маршруты для API

app.UseStaticFiles();

// Убираем UseMvc и добавляем UseEndpoints
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();  // Маппинг контроллеров через Endpoint Routing
});

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during authentication.");
        throw;
    }
});

app.Run();
