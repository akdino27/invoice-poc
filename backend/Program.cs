using invoice_v1.src.Api.Filters;
using invoice_v1.src.Api.Middleware;
using invoice_v1.src.Application.BackgroundServices;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Application.Services;
using invoice_v1.src.Infrastructure.Data;
using invoice_v1.src.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //Validate critical configuration at startup
            ValidateConfiguration(builder.Configuration);

            // Add services to the container
            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigurePipeline(app);

            app.Run();
        }

        private static void ValidateConfiguration(IConfiguration configuration)
        {
            var errors = new List<string>();

            // Validate callback secret
            var callbackSecret = configuration["Security:CallbackSecret"];
            if (string.IsNullOrWhiteSpace(callbackSecret))
            {
                errors.Add("Security:CallbackSecret is not configured. Set AI_CALLBACK_SECRET environment variable.");
            }

            // Validate admin API key
            var adminApiKey = configuration["Security:AdminApiKey"];
            if (string.IsNullOrWhiteSpace(adminApiKey))
            {
                errors.Add("Security:AdminApiKey is not configured. Set ADMIN_API_KEY environment variable.");
            }

            // Validate admin emails
            var adminEmails = configuration.GetSection("Security:AdminEmails").Get<List<string>>();
            if (adminEmails == null || adminEmails.Count == 0)
            {
                errors.Add("Security:AdminEmails is not configured. At least one admin email is required.");
            }
            else
            {
                // Validate email format
                foreach (var email in adminEmails)
                {
                    if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                    {
                        errors.Add($"Invalid admin email format: '{email}'");
                    }
                }
            }

            // Validate database connection string
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                errors.Add("Database connection string 'DefaultConnection' is not configured.");
            }

            if (errors.Count > 0)
            {
                var errorMessage = "Configuration validation failed:\n" + string.Join("\n", errors);
                Console.Error.WriteLine(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            Console.WriteLine("✓ Configuration validation passed");
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Database - PostgreSQL
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions
                        .EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorCodesToAdd: null)));  // FIXED: Added required parameter

            // Controllers
            services.AddControllers();

            // Swagger/OpenAPI
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "Invoice API with RBAC - PostgreSQL", Version = "v1" });

                // Add header parameters for Swagger UI testing
                c.AddSecurityDefinition("UserEmail", new()
                {
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Name = "X-User-Email",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Description = "User email for RBAC (e.g., vendor1@test.com)"
                });

                c.AddSecurityDefinition("UserRole", new()
                {
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Name = "X-User-Role",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Description = "User role: 'vendor' or 'admin'"
                });

                c.AddSecurityRequirement(new()
                {
                    {
                        new()
                        {
                            Reference = new()
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "UserEmail"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // CORS (if needed)
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            //Register RBAC Action Filter
            services.AddScoped<RbacActionFilter>();

            // Repositories
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IJobRepository, JobRepository>();
            services.AddScoped<IFileChangeLogRepository, FileChangeLogRepository>();
            services.AddScoped<IVendorRepository, VendorRepository>();
            services.AddScoped<IInvalidInvoiceRepository, InvalidInvoiceRepository>();

            // Services
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IJobService, JobService>();
            services.AddScoped<ICallbackService, CallbackService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();
            services.AddScoped<IVendorService, VendorService>();
            services.AddScoped<ILogService, LogService>();
            services.AddScoped<IInvalidInvoiceService, InvalidInvoiceService>();
            services.AddScoped<IHmacValidator, HmacValidator>();

            // Background Services
            services.AddHostedService<JobCreationService>();

            // Logging
            services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            });
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            //Add exception handling middleware first
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Invoice API v1");
                    c.RoutePrefix = string.Empty; // Swagger at root
                });
            }

            app.UseHttpsRedirection();

            app.UseCors();

            app.UseAuthorization();

            app.MapControllers();

            // Health check endpoint
            app.MapGet("/health", () => new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "2.0-RBAC-PostgreSQL",
                database = "PostgreSQL"
            });

            Console.WriteLine("✓ Application configured successfully");
            Console.WriteLine("✓ RBAC is enabled on all protected endpoints");
            Console.WriteLine("✓ Using PostgreSQL database with JSONB support");
        }
    }
}
