using CharonGateway.Configuration;
using CharonGateway.GraphQL.Queries;
using CharonGateway.GraphQL.Types;
using CharonGateway.Middleware;
using CharonGateway.Middleware.Interfaces;
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

    // Register middleware services
    builder.Services.AddScoped<ILoggingService, LoggingService>();
    builder.Services.AddScoped<IExceptionHandlingService, ExceptionHandlingService>();

    // Register repositories
    builder.Services.AddScoped<IMetricRepository, MetricRepository>();

    // Register services with decorators (order matters: validation -> exception handling)
    builder.Services.AddScoped<MetricService>();
    builder.Services.AddScoped<IMetricService>(serviceProvider =>
    {
        var inner = serviceProvider.GetRequiredService<MetricService>();
        var exceptionHandling = serviceProvider.GetRequiredService<IExceptionHandlingService>();
        
        // Apply decorators in order: Validation -> Exception Handling
        var withValidation = new ValidationDecorator(inner);
        return new MetricServiceDecorator(withValidation, exceptionHandling);
    });

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
            // Increase complexity limit to handle date-filtered queries with payload
            // Default is 1000, but queries with payload field can reach ~2652
            // Use reflection to find and set the complexity limit
            var optionsType = options.GetType();
            var complexityProperty = optionsType.GetProperty("Complexity");
            if (complexityProperty != null)
            {
                var complexityObj = complexityProperty.GetValue(options);
                if (complexityObj != null)
                {
                    var complexityType = complexityObj.GetType();
                    var maxAllowedProperty = complexityType.GetProperty("MaxAllowedComplexity") 
                        ?? complexityType.GetProperty("MaximumAllowed")
                        ?? complexityType.GetProperty("MaximumComplexity");
                    if (maxAllowedProperty != null && maxAllowedProperty.CanWrite)
                    {
                        maxAllowedProperty.SetValue(complexityObj, 3000);
                        Log.Information($"Set complexity limit to 3000 via reflection");
                    }
                }
            }
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
