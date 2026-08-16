using System.Text;
using System.Threading.RateLimiting;
using FinTrack.Api;
using FinTrack.Api.Middleware;
using FinTrack.Api.Services;
using FinTrack.Application;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Common.Security;
using FinTrack.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging via Serilog, configured from appsettings.
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Bind JWT settings from configuration (Secret comes from user-secrets / environment).
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");
if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
{
    throw new InvalidOperationException(
        "Jwt:Secret is not configured. Set it via user-secrets or the Jwt__Secret environment variable.");
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();

// JWT bearer authentication.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Rate limiting: protect auth endpoints from brute-force / abuse.
// Limits are configurable so integration tests can raise them (config key "RateLimiting:Auth").
var authPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:Auth:PermitLimit") ?? 10;
var authWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:Auth:WindowSeconds") ?? 60;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(RateLimitingPolicies.Auth, limiter =>
    {
        limiter.PermitLimit = authPermitLimit;
        limiter.Window = TimeSpan.FromSeconds(authWindowSeconds);
        limiter.QueueLimit = 0;
    });
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize/deserialize enums as their names ("Income"/"Expense") rather than numbers.
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "FinTrack AI API", Version = "v1" });

    // Enables the "Authorize" button in Swagger UI for bearer tokens.
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token (without the 'Bearer ' prefix).",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

const string corsPolicy = "FinTrackCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// Global error handling must be the outermost middleware.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseCors(corsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposed so the integration test project (Phase 4+) can use WebApplicationFactory<Program>.
public partial class Program;
