using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuoteManager.Models;

namespace QuoteManager.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Quote> Quotes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Existing: Configure self-referencing relationship
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.CreatedBy)
                .WithMany(u => u.CreatedUsers)
                .HasForeignKey(u => u.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // NEW: Configure Quote relationships
            builder.Entity<Quote>()
                .HasOne(q => q.CreatedBy)
                .WithMany()
                .HasForeignKey(q => q.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Quote>()
                .HasOne(q => q.Client)
                .WithMany()
                .HasForeignKey(q => q.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
    }