using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using ProductApi.Application;
using ProductApi.Domain.Ports;
using ProductApi.Infrastructure.Auth;
using ProductApi.Infrastructure.Messaging;
using ProductApi.Infrastructure.Persistence;
using ProductApi.Infrastructure.RateLimiting;
using ProductApi.Api.Middleware;
using Prometheus;
using StackExchange.Redis;

// QuestPDF Community license: free for organizations with < $1M USD annual gross revenue —
// fine for a portfolio project (same "document the tradeoff" pattern as the demo JWT keypair
// and QuestPDF/ClosedXML use — see README).
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Port=5432;Database=productdb;Username=product_user;Password=product_pass";
builder.Services.AddDbContext<ProductDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<IProductRepository>(sp => new CachedProductRepository(
    sp.GetRequiredService<ProductRepository>(),
    sp.GetRequiredService<IConnectionMultiplexer>(),
    sp.GetRequiredService<ILogger<CachedProductRepository>>()));
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddSingleton<DemoUserStore>();

// JWT signing key: prefer a PEM supplied externally (Jwt__PrivateKeyPem, or a file path via
// Jwt__PrivateKeyPath — e.g. a mounted K8s Secret, so every replica signs/verifies with the
// same key), otherwise generate one in-process. Generating is fine for local dev/tests/CI
// (single process, signs and verifies with the same RSA instance) but NOT safe for a real
// multi-replica deploy without one of those two configured — a pod that didn't sign a token
// can't verify it, since each pod would otherwise get its own random key.
var configuredPem = builder.Configuration["Jwt:PrivateKeyPem"]
    ?? (builder.Configuration["Jwt:PrivateKeyPath"] is { Length: > 0 } keyPath && File.Exists(keyPath)
        ? await File.ReadAllTextAsync(keyPath)
        : null);

var jwtRsa = RSA.Create(2048);
if (configuredPem is not null)
{
    jwtRsa.ImportFromPem(configuredPem);
}
builder.Services.AddSingleton(new JwtTokenService(jwtRsa));

var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
var kafkaUsername = builder.Configuration["Kafka:Username"];
var kafkaPassword = builder.Configuration["Kafka:Password"];
builder.Services.AddSingleton<IProductEventPublisher>(sp =>
    new KafkaProductEventPublisher(kafkaBootstrapServers,
        sp.GetRequiredService<ILogger<KafkaProductEventPublisher>>(), kafkaUsername, kafkaPassword));

var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = JwtTokenService.Issuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(jwtRsa)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Liveness: process is up, no dependency checks (cheap, matches the Quarkus/Spring
// siblings' /q/health/live and /actuator/health/liveness — never fails on a downstream outage).
// Readiness: verifies Postgres connectivity, matches the siblings' /q/health/ready and
// /actuator/health/readiness — a pod that can't reach its DB should stop receiving traffic.
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<ProductDbContext>(name: "postgres", tags: ["ready"]);

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var migrationScope = app.Services.CreateScope();
    await migrationScope.ServiceProvider.GetRequiredService<ProductDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpMetrics();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapMetrics();

await app.RunAsync();

namespace ProductApi.Api
{
    public partial class Program;
}
