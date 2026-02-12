using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Application.Services;
using invoice_v1.src.Infrastructure.Data;
using invoice_v1.src.Infrastructure.Repositories;
using invoice_v1.src.Application.BackgroundServices;
using invoice_v1.src.Application.Security;
using invoice_v1.src.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


using Npgsql;

var builder = WebApplication.CreateBuilder(args);

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);




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
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);

        npgsqlOptions.CommandTimeout(120);
    });
});

// REPOSITORIES

builder.Services.AddScoped<IJobRepository, JobRepository>();


// APPLICATION SERVICES

builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddSingleton<IHmacValidator, HmacValidator>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

//JWT


builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"]!;

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)),

            ClockSkew = TimeSpan.Zero // IMPORTANT: no silent expiry grace
        };
    });


// V1 SERVICES (Preserved)

builder.Services.AddSingleton<IGoogleDriveService, GoogleDriveService>();


// BACKGROUND SERVICES

// Drive Monitoring
builder.Services.AddHostedService<DriveMonitoringService>();

// Job Creation from FileChangeLogs
builder.Services.AddHostedService<JobCreationService>();

builder.Services.AddScoped<IVendorInvoiceService, VendorInvoiceService>();

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddScoped<AdminBootstrapService>();


//Admin approval
builder.Services.AddScoped<IAdminUserService, AdminUserService>();




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

builder.Services.AddScoped<IAuthService, AuthService>();


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

    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT token"
    });

    c.AddSecurityRequirement(new()
{
    {
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Reference = new Microsoft.OpenApi.Models.OpenApiReference
            {
                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        Array.Empty<string>()
    }
});


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


//First Admin
using (var scope = app.Services.CreateScope())
{
    var bootstrap = scope.ServiceProvider
        .GetRequiredService<AdminBootstrapService>();

    await bootstrap.EnsureAdminExistsAsync();
}



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


app.UseAuthentication();
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
