using CharonGateway.Configuration;
using CharonGateway.GraphQL.Queries;
using CharonGateway.GraphQL.Types;
using CharonGateway.Middleware;
using CharonGateway.Repositories;
using CharonGateway.Repositories.Interfaces;
using CharonGateway.Services;
using CharonGateway.Services.Decorators;
using CharonGateway.Services.Interfaces;
using CharonDbContext.Data;
using FluentValidation;
using HotChocolate.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using CharonGateway.Repositories.Decorators;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "CharonGateway")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/charon-gateway-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Charon GraphQL Gateway");

    var builder = WebApplication.CreateBuilder(args);
    
    builder.Host.UseSerilog();
    
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Ensure camelCase JSON serialization (Items -> items)
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.Configure<DatabaseOptions>(
        builder.Configuration.GetSection(DatabaseOptions.SectionName));

    var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>();
    // Skip SqlServer registration in Testing environment to allow tests to use InMemory
    if (!string.IsNullOrEmpty(databaseOptions?.ConnectionString) && 
        !builder.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(databaseOptions.ConnectionString));
    }

    // Register FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // Register repositories with logging decorator
    builder.Services.AddScoped<MetricRepository>();
    builder.Services.AddScoped<IMetricRepository>(serviceProvider =>
    {
        var inner = serviceProvider.GetRequiredService<MetricRepository>();
        var logger = serviceProvider.GetRequiredService<ILogger<LoggingMetricRepositoryDecorator>>();
        return new LoggingMetricRepositoryDecorator(inner, logger);
    });

    // Register services with decorators (validation only)
    // Exception handling is done by GlobalExceptionHandlerMiddleware at the HTTP level
    builder.Services.AddScoped<MetricService>();
    builder.Services.AddScoped<IMetricService>(serviceProvider =>
    {
        var inner = serviceProvider.GetRequiredService<MetricService>();
        // Apply validation decorator only
        return new ValidationDecorator(inner);
    });

    // Configure GraphQL server
    // Note: Date-filtered queries should use REST API (/api/metrics) to avoid complexity limits
    builder.Services
        .AddGraphQLServer()
        .AddQueryType()
        .AddTypeExtension<MetricQueries>()
        .AddType<MetricType>()
        .AddType<DailyAverageMetricType>()
        .AddFiltering()
        .AddSorting()
        .AddProjections()
        .AddType<MetricsAggregation>()
        .AddType<TypeAggregation>()
        .ModifyPagingOptions(options =>
        {
            options.IncludeTotalCount = true;
        })
        .ModifyRequestOptions(options =>
        {
            options.ExecutionTimeout = TimeSpan.FromSeconds(30);
        });

    var app = builder.Build();
    
    // Ensure database and tables exist (if connection string is provided)
    // Skip in test environment to avoid conflicts with test databases
    if (!string.IsNullOrEmpty(databaseOptions?.ConnectionString) && 
        !app.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.EnsureCreatedAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to ensure database is created. This may be expected in test environments.");
        }
    }
    
    // Configure middleware in correct order
    // Add global exception handler middleware first (catches all exceptions)
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    
    // HTTPS redirection (may redirect HTTP to HTTPS, but should not affect API calls)
    app.UseHttpsRedirection();
    
    // Authorization
    app.UseAuthorization();
    
    // Map API controllers
    app.MapControllers();
    
    // Map GraphQL endpoint (after controllers to avoid conflicts)
    if (app.Environment.IsDevelopment())
    {
        app.MapGraphQL().WithOptions(new GraphQLServerOptions
        {
            Tool = { Enable = true }
        });
    }
    else
    {
        app.MapGraphQL();
    }
    
    Log.Information("Charon GraphQL Gateway started successfully");
    
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Charon GraphQL Gateway terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
