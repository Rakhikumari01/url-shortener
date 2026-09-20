using Microsoft.EntityFrameworkCore;
using UrlShortener.Data;
using UrlShortener.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services.
builder.Services.AddControllers();

// EF Core - PostgreSQL (connection string from configuration)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var conn = builder.Configuration.GetConnectionString("Postgres");
    if (string.IsNullOrWhiteSpace(conn))
    {
        // Fail fast with clear message during development if missing.
        throw new InvalidOperationException("Connection string 'Postgres' is required.");
    }
    options.UseNpgsql(conn);
});

// Application services
builder.Services.AddScoped<IUrlService, UrlService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Logger.LogInformation("UrlShortener starting up.");

// Enable Swagger in Development or when explicitly enabled via config.
var swaggerEnabled = app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Do not enforce HTTPS redirection in container environments.

// Authorization middleware (no auth configured; kept to allow future policies)
app.UseAuthorization();

app.MapControllers();

app.Run();
