using invoice_v1.src.Api.Middleware;
using invoice_v1.src.Application.BackgroundServices;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Application.Security;
using invoice_v1.src.Application.Services;
using invoice_v1.src.Infrastructure.Data;
using invoice_v1.src.Infrastructure.Repositories;
using invoice_v1.src.Infrastructure.Services;
using invoice_v1.src.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);


// DATABASE CONFIGURATION

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    }));


// REDIS DISTRIBUTED CACHE

var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "InvoiceApp_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
    Console.WriteLine("WARNING: Using in-memory cache. Configure Redis for production.");
}


// AUTHENTICATION & AUTHORIZATION

var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.Configure<JwtOptions>(jwtSettings);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();


// DEPENDENCY INJECTION - REPOSITORIES (Scoped)

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IFileChangeLogRepository, FileChangeLogRepository>();
builder.Services.AddScoped<IInvalidInvoiceRepository, InvalidInvoiceRepository>();
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();


// DEPENDENCY INJECTION - APPLICATION SERVICES (Scoped)

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IVendorInvoiceService, VendorInvoiceService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IFileChangeLogService, FileChangeLogService>();
builder.Services.AddScoped<IRateLimitService, RateLimitService>();


// DEPENDENCY INJECTION - SECURITY (Singleton)

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IHmacValidator, HmacValidator>();


// DEPENDENCY INJECTION - EXTERNAL SERVICES

builder.Services.AddSingleton<IGoogleDriveService, GoogleDriveService>();

// FIX: Correct WorkerClient registration
builder.Services.AddHttpClient(); // Register IHttpClientFactory
builder.Services.AddScoped<IWorkerClient, WorkerClient>(); // Register WorkerClient normally


// BACKGROUND SERVICES

builder.Services.AddHostedService<JobCreationService>();
builder.Services.AddHostedService<JobRetryService>();
builder.Services.AddHostedService<DriveMonitoringService>();


// ADMIN BOOTSTRAP SERVICE

builder.Services.AddScoped<AdminBootstrapService>();


// CORS CONFIGURATION - Allow both localhost ports

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",  // Angular dev server
                "http://localhost:4201",  // Alternative port
                "https://localhost:4200",
                "https://localhost:4201"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .WithExposedHeaders("Token-Expired"); // Expose custom headers
    });
});


// CONTROLLERS & API CONFIGURATION - FIXED: camelCase JSON serialization

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // FIX: Use camelCase for JSON properties
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;

        // Additional options for better frontend compatibility
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();

        // Handle circular references
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });


// SWAGGER/OPENAPI

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Invoice Processing API",
        Version = "v1",
        Description = "API for automated invoice extraction and management"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});


// LOGGING

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsProduction())
{
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
}
else
{
    // More verbose logging in development
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
}

var app = builder.Build();


// DATABASE MIGRATION & ADMIN BOOTSTRAP

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
        logger.LogInformation(" Database migration completed");

        var bootstrapService = services.GetRequiredService<AdminBootstrapService>();
        await bootstrapService.EnsureAdminExistsAsync();
        logger.LogInformation(" Admin bootstrap completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error during startup initialization");
        throw;
    }
}


// MIDDLEWARE PIPELINE

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Invoice API v1");
        c.RoutePrefix = string.Empty;
    });

    // Log startup info
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("🚀 Swagger UI available at: https://localhost:{Port}",
        builder.Configuration["Kestrel:Endpoints:Https:Url"] ?? "5247");
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseHttpsRedirection();

// CORS - Must be before Authentication/Authorization
app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Log registered routes in development
if (app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation(" Application started successfully");
}

app.Run();
