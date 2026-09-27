using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Data;
using UrlShortener.HealthChecks;
using UrlShortener.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// UrlService needs the incoming request to derive the public origin when
// BaseUrl is not explicitly configured.
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var conn = builder.Configuration.GetConnectionString("Postgres");
    if (string.IsNullOrWhiteSpace(conn))
    {
        throw new InvalidOperationException(
            "Connection string 'Postgres' is required. In containers set ConnectionStrings__Postgres; " +
            "locally use `dotnet user-secrets set \"ConnectionStrings:Postgres\" \"...\"`.");
    }

    // Postgres restarts (routine in compose and during node drains) surface as
    // transient errors; retry rather than 500. Note this installs an execution
    // strategy, so any future explicit transaction must be wrapped in
    // Database.CreateExecutionStrategy().ExecuteAsync(...).
    options.UseNpgsql(conn, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 5));
});

builder.Services.AddScoped<IUrlService, UrlService>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });

// Behind an ingress or reverse proxy the app sees http on an internal port.
// Without this, generated short URLs come back with the wrong scheme and host.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                               | ForwardedHeaders.XForwardedProto
                               | ForwardedHeaders.XForwardedHost;

    // Container networks hand out ephemeral addresses, so the proxy cannot be
    // pinned by IP here. Narrow these two once the proxy has a stable address -
    // as written, anything that can reach the pod can spoof X-Forwarded-*.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseForwardedHeaders();

var swaggerEnabled = app.Environment.IsDevelopment()
                     || builder.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Liveness: is the process answering at all? No dependency checks, so a
// database blip cannot trigger a pointless container restart loop.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: can this instance actually serve traffic right now?
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.MapControllers();

app.Logger.LogInformation(
    "UrlShortener started. Environment={Environment} Swagger={SwaggerEnabled}",
    app.Environment.EnvironmentName,
    swaggerEnabled);

app.Run();
