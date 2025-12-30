using CharonGateway.Configuration;
using CharonGateway.GraphQL.Queries;
using CharonGateway.GraphQL.Types;
using CharonGateway.Hubs;
using CharonDbContext.Data;
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
    
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.Configure<DatabaseOptions>(
        builder.Configuration.GetSection(DatabaseOptions.SectionName));

    var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>();
    if (!string.IsNullOrEmpty(databaseOptions?.ConnectionString))
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(databaseOptions.ConnectionString));
    }

    builder.Services
        .AddGraphQLServer()
        .AddQueryType()
        .AddTypeExtension<MetricQueries>()
        .AddType<MetricType>()
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

    builder.Services.AddSignalR();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins("http://localhost:4200", "http://localhost:5005")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    var app = builder.Build();
    
    // CORS must be before other middleware
    app.UseCors("AllowFrontend");
    
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

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<MetricsHub>("/metricsHub");
    
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

