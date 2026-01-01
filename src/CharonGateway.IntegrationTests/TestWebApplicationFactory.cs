using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CharonDbContext.Data;

namespace CharonGateway.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<CharonGateway.Program>
{
    private readonly string _testDbName = "TestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        // Configure app configuration FIRST, before any services are registered
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Build a temporary configuration to check what's being loaded
            var tempConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Database:ConnectionString", string.Empty }
                })
                .Build();
            
            // Clear all sources and add our test configuration
            config.Sources.Clear();
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:ConnectionString", string.Empty }, // Explicitly empty
                { "Logging:LogLevel:Default", "Warning" },
                { "AllowedHosts", "*" }
            });
        });

        // Configure services AFTER app configuration
        builder.ConfigureServices((context, services) =>
        {
            // Remove existing DbContext registrations that might have been added by Program.cs
            // This runs AFTER Program.cs has executed, so we need to replace what was registered
            var descriptors = services.Where(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                     d.ServiceType == typeof(ApplicationDbContext))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Register in-memory database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_testDbName);
            }, ServiceLifetime.Scoped);
        });
    }

    public string TestDbName => _testDbName;
}

