using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Data;
using QuoteManager.Models;
using QuoteManager.Services;

namespace QuoteManager
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add database connection
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Configure Identity
            builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
                options.SignIn.RequireConfirmedAccount = false)  // ← IMPORTANT: false = can login immediately
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();





            // Configure cookie authentication for role-based redirects
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.LoginPath = "/Identity/Account/Login";
            });





            builder.Services.AddRazorPages();

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdminRole", policy =>
                    policy.RequireRole("SuperAdmin", "Admin"));

                options.AddPolicy("RequireStaffRole", policy =>
                    policy.RequireRole("SuperAdmin", "Admin", "Staff"));
            });

            var app = builder.Build();

            // Seed roles and SuperAdmin
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                await DbSeeder.SeedRolesAndSuperAdmin(services);
            }

            // Configure middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.MapRazorPages();

            app.Run();
        }
    }
}