using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductApi.Domain.Ports;
using ProductApi.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ProductApi.IntegrationTests;

public sealed class ProductsApiFactory : WebApplicationFactory<ProductApi.Api.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("productdb")
        .WithUsername("product_user")
        .WithPassword("product_pass")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var noOpPublisher = services.SingleOrDefault(d => d.ServiceType == typeof(IProductEventPublisher));
            if (noOpPublisher is not null) services.Remove(noOpPublisher);
            services.AddSingleton<IProductEventPublisher, NoOpProductEventPublisher>();
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Environment variables are read reliably by the default configuration chain regardless of
        // WebApplicationFactory/minimal-hosting-model timing quirks around ConfigureAppConfiguration,
        // unlike an in-memory config source added via ConfigureWebHost (which was observed NOT to be
        // visible yet when top-level Program.cs reads builder.Configuration eagerly at startup).
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _postgres.GetConnectionString());
        // abortConnect=false so StackExchange.Redis defers connecting instead of throwing at startup
        // when no broker is available; RateLimitingMiddleware fails open on any Redis error anyway.
        Environment.SetEnvironmentVariable("Redis__ConnectionString", "localhost:6390,abortConnect=false,connectTimeout=200");
        Environment.SetEnvironmentVariable("Kafka__BootstrapServers", "localhost:1");

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.StopAsync();
    }
}

file sealed class NoOpProductEventPublisher : IProductEventPublisher
{
    public Task PublishAsync(ProductApi.Domain.Events.ProductEvent @event, CancellationToken ct = default) =>
        Task.CompletedTask;
}
