using FinTrack.Api.Middleware;
using FinTrack.Api.Services;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging via Serilog, configured from appsettings.
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Infrastructure: EF Core + PostgreSQL/pgvector.
builder.Services.AddInfrastructure(builder.Configuration);

// Current-user resolution from HTTP context.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();

builder.Services.AddControllers();
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "FinTrack AI API", Version = "v1" });
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
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposed so the integration test project (Phase 4+) can use WebApplicationFactory<Program>.
public partial class Program;
