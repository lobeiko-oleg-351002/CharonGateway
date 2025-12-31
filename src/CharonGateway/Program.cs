using CharonGateway.Configuration;
using CharonGateway.GraphQL.Queries;
using CharonGateway.GraphQL.Types;
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

    var app = builder.Build();
    
    // Ensure database and tables exist (if connection string is provided)
    if (!string.IsNullOrEmpty(databaseOptions?.ConnectionString))
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }
    }
    
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

