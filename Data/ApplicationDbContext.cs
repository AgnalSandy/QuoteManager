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

        public DbSet<TaxMaster> TaxMasters { get; set; }
        public DbSet<ServiceMaster> ServiceMasters { get; set; }
        public DbSet<ServiceTax> ServiceTaxes { get; set; }
        public DbSet<QuoteItem> QuoteItems { get; set; }
        public DbSet<QuoteItemTax> QuoteItemTaxes { get; set; }
        public DbSet<CompanySettings> CompanySettings { get; set; }
        public DbSet<Invoice> Invoices { get; set; }

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





            // Service Master relationships
            builder.Entity<ServiceMaster>()
                .HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);

            // Service-Tax mapping (many-to-many)
            builder.Entity<ServiceTax>()
                .HasOne(st => st.Service)
                .WithMany(s => s.ServiceTaxes)
                .HasForeignKey(st => st.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ServiceTax>()
                .HasOne(st => st.Tax)
                .WithMany(t => t.ServiceTaxes)
                .HasForeignKey(st => st.TaxId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quote Item relationships
            builder.Entity<QuoteItem>()
                .HasOne(qi => qi.Quote)
                .WithMany(q => q.QuoteItems)
                .HasForeignKey(qi => qi.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<QuoteItem>()
                .HasOne(qi => qi.Service)
                .WithMany(s => s.QuoteItems)
                .HasForeignKey(qi => qi.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quote Item Tax relationships
            builder.Entity<QuoteItemTax>()
                .HasOne(qit => qit.QuoteItem)
                .WithMany(qi => qi.QuoteItemTaxes)
                .HasForeignKey(qit => qit.QuoteItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<QuoteItemTax>()
                .HasOne(qit => qit.Tax)
                .WithMany()
                .HasForeignKey(qit => qit.TaxId)
                .OnDelete(DeleteBehavior.Restrict);

            // Invoice relationships
            builder.Entity<Invoice>()
                .HasOne(i => i.Quote)
                .WithMany()
                .HasForeignKey(i => i.QuoteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Invoice>()
                .HasOne(i => i.Client)
                .WithMany()
                .HasForeignKey(i => i.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Invoice>()
                .HasOne(i => i.PreparedBy)
                .WithMany()
                .HasForeignKey(i => i.PreparedById)
                .OnDelete(DeleteBehavior.SetNull);

            // Company Settings
            builder.Entity<CompanySettings>()
                .HasOne(cs => cs.UpdatedBy)
                .WithMany()
                .HasForeignKey(cs => cs.UpdatedById)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique constraints
            builder.Entity<TaxMaster>()
                .HasIndex(t => t.TaxName)
                .IsUnique();

            builder.Entity<ServiceMaster>()
                .HasIndex(s => s.ServiceName)
                .IsUnique();



        }
    }
    }