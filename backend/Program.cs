using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Application.Services;
using invoice_v1.src.Infrastructure.Data;
using invoice_v1.src.Infrastructure.Repositories;
using invoice_v1.src.Application.BackgroundServices;
using invoice_v1.src.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


// SERILOG CONFIGURATION

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/invoice-v1-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

Log.Information("Starting Invoice Processing Backend V2");


// DATABASE CONFIGURATION

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(120);
    });
});


// REPOSITORIES

builder.Services.AddScoped<IJobRepository, JobRepository>();


// APPLICATION SERVICES

builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddSingleton<IHmacValidator, HmacValidator>();


// V1 SERVICES (Preserved)

builder.Services.AddSingleton<IGoogleDriveService, GoogleDriveService>();


// BACKGROUND SERVICES

// Drive Monitoring
builder.Services.AddHostedService<DriveMonitoringService>();

// Job Creation from FileChangeLogs
builder.Services.AddHostedService<JobCreationService>();


// CORS CONFIGURATION

var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',')
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


// CONTROLLERS AND SWAGGER

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // PascalCase
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Invoice Processing API V2",
        Version = "v2.0",
        Description = "Backend API for invoice extraction and analytics",
        Contact = new()
        {
            Name = "Support",
            Email = "support@example.com"
        }
    });

    // Add XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add security definition for API Key
    c.AddSecurityDefinition("ApiKey", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-Api-Key",
        Description = "API Key for admin endpoints"
    });
});


// BUILD APPLICATION

var app = builder.Build();


// MIDDLEWARE PIPELINE


// Exception handling
app.UseMiddleware<invoice_v1.src.Api.Middleware.ExceptionHandlingMiddleware>();

// Swagger (enable in development, disable in production)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Invoice API V2");
        c.RoutePrefix = "swagger";
    });
}

// Request logging
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
    };
});

// HTTPS redirection (disable for local Docker, enable for production)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// CORS
app.UseCors("AllowFrontend");

// Authorization (for future JWT implementation)
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Version = "2.0",
    Environment = app.Environment.EnvironmentName
}));

// Root endpoint
app.MapGet("/", () => Results.Redirect("/swagger"));


try
{
    Log.Information("Invoice Processing Backend V2 is starting");
    app.Run();
    Log.Information("Invoice Processing Backend V2 stopped cleanly");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Invoice Processing Backend V2 terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
