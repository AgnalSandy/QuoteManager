using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Application.Services;
using QuoteManager.Constants;
using QuoteManager.Core.Interfaces;
using QuoteManager.Core.Interfaces.Services;
using QuoteManager.Data;
using QuoteManager.Infrastructure.Middleware;
using QuoteManager.Infrastructure.Repositories;
using QuoteManager.Models;
using QuoteManager.Services;
using Serilog;
using Serilog.Events;
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace QuoteManager
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Configure Serilog for enterprise-grade logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/quotemanager-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting QuoteManager application");

                var builder = WebApplication.CreateBuilder(args);

                // Add Serilog
                builder.Host.UseSerilog();

                // Add database connection
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseSqlServer(connectionString);
                    
                    if (builder.Environment.IsDevelopment())
                    {
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                    }
                });

                builder.Services.AddDatabaseDeveloperPageExceptionFilter();

                // Configure Identity with production-grade settings
                builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    
                    // Password settings
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequiredLength = 8;
                    
                    // Lockout settings
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;
                    
                    // User settings
                    options.User.RequireUniqueEmail = true;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

                // Configure cookie authentication with security hardening
                builder.Services.ConfigureApplicationCookie(options =>
                {
                    options.AccessDeniedPath = "/AccessDenied";
                    options.LoginPath = "/Identity/Account/Login";
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(24);
                    
                    // Security settings
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                });

                // Add Memory Cache for performance
                builder.Services.AddMemoryCache();

                // Add Response Caching
                builder.Services.AddResponseCaching();

                // Add Response Compression with Brotli (OPTIMIZED)
                builder.Services.AddResponseCompression(options =>
                {
                    options.EnableForHttps = true;
                    options.Providers.Add<BrotliCompressionProvider>();
                    options.Providers.Add<GzipCompressionProvider>();
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
                    {
                        "text/plain",
                        "text/css",
                        "application/javascript",
                        "text/javascript",
                        "application/json",
                        "text/html",
                        "image/svg+xml",
                        "application/xml"
                    });
                });

                // Configure Brotli compression level
                builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
                {
                    options.Level = CompressionLevel.Fastest;
                });

                // Configure Gzip compression level
                builder.Services.Configure<GzipCompressionProviderOptions>(options =>
                {
                    options.Level = CompressionLevel.Optimal;
                });

                // Register Unit of Work and Repositories
                builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

                // Register Application Services
                builder.Services.AddScoped<ICacheService, CacheService>();
                builder.Services.AddScoped<IQuoteService, QuoteService>();
                builder.Services.AddScoped<IMasterDataService, MasterDataService>();
                builder.Services.AddScoped<IQuoteNumberGenerator, QuoteNumberGenerator>();
                builder.Services.AddScoped<IInvoiceNumberGenerator, InvoiceNumberGenerator>();

                // Add health checks
                builder.Services.AddHealthChecks()
                    .AddDbContextCheck<ApplicationDbContext>("database")
                    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

                builder.Services.AddRazorPages(options =>
                {
                    // Global authorization
                    options.Conventions.AuthorizeFolder("/");
                    options.Conventions.AllowAnonymousToPage("/Index");
                    options.Conventions.AllowAnonymousToPage("/Privacy");
                    options.Conventions.AllowAnonymousToPage("/Error");
                });

                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy(ApplicationRoles.Policies.RequireAdminRole, policy =>
                        policy.RequireRole(ApplicationRoles.SuperAdmin, ApplicationRoles.Admin));

                    options.AddPolicy(ApplicationRoles.Policies.RequireStaffRole, policy =>
                        policy.RequireRole(ApplicationRoles.SuperAdmin, ApplicationRoles.Admin, ApplicationRoles.Staff));
                });

                // Add session
                builder.Services.AddSession(options =>
                {
                    options.IdleTimeout = TimeSpan.FromMinutes(30);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                });

                var app = builder.Build();

                // Seed database
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    
                    try
                    {
                        await DbSeeder.SeedRolesAndSuperAdmin(services);
                        await DbSeeder.SeedMasterData(services);
                        
                        Log.Information("Database seeding completed successfully");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "An error occurred while seeding the database");
                        throw;
                    }
                }

                // Configure middleware pipeline
                if (app.Environment.IsDevelopment())
                {
                    app.UseMigrationsEndPoint();
                }
                else
                {
                    app.UseMiddleware<GlobalExceptionMiddleware>();
                    app.UseHsts();
                }

                // Add security headers
                app.UseMiddleware<SecurityHeadersMiddleware>();

                // Add performance monitoring
                app.UseMiddleware<PerformanceMonitoringMiddleware>();

                // Force HTTPS
                app.UseHttpsRedirection();

                // Response compression (before static files)
                app.UseResponseCompression();

                // OPTIMIZED: Static files with smart caching
                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = ctx =>
                    {
                        // Cache static assets for 7 days (images, css, js, fonts)
                        var path = ctx.File.PhysicalPath;
                        if (path != null)
                        {
                            var extension = Path.GetExtension(path).ToLowerInvariant();
                            var cacheableExtensions = new[] { ".css", ".js", ".jpg", ".jpeg", ".png", ".gif", ".svg", ".woff", ".woff2", ".ttf", ".eot" };
                            
                            if (cacheableExtensions.Contains(extension))
                            {
                                // Cache for 7 days
                                ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800,immutable");
                            }
                            else
                            {
                                // No cache for HTML and other files
                                ctx.Context.Response.Headers.Append("Cache-Control", "no-cache,no-store,must-revalidate");
                            }
                        }
                    }
                });

                app.UseResponseCaching();
                app.UseRouting();
                app.UseSession();
                app.UseAuthentication();
                app.UseAuthorization();

                // Health checks endpoint
                app.MapHealthChecks("/health");

                app.MapRazorPages();

                Log.Information("QuoteManager application started successfully");
                
                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}