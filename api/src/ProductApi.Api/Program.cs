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
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<ProductDbContext>(name: "postgres", tags: ["ready"]);

var app = builder.Build();

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var migrationScope = app.Services.CreateScope();
    startupLogger.LogInformation("Applying pending EF Core migrations");
    await migrationScope.ServiceProvider.GetRequiredService<ProductDbContext>().Database.MigrateAsync();
    startupLogger.LogInformation("EF Core migrations applied");
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

startupLogger.LogInformation("ProductApi starting in {Environment} environment", app.Environment.EnvironmentName);
await app.RunAsync();

namespace ProductApi.Api
{
    public partial class Program;
}
