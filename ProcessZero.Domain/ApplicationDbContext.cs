using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Design-time constructor for migrations
        public ApplicationDbContext() : base(DesignTimeDbContextOptions())
        {
        }

        private static DbContextOptions<ApplicationDbContext> DesignTimeDbContextOptions()
        {
            // Build the path to the PetGroomer.Api project
            var webProjectPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, "ProcessZero.Api");

            // Load the configuration from appsettings.json in the .Api project
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(webProjectPath) // Set the base path to the .Api directory
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Load appsettings.json
                .Build();

            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Choose the appropriate database provider here
            builder.UseSqlServer(connectionString); // Or UseSqlite, UseNpgsql, etc.

            return builder.Options;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ──────────────────────────────────────────────────────────
            // Column length constraints (required for indexing nvarchar)
            // ──────────────────────────────────────────────────────────

            // Contacts — constrain UserId & Email so they can be indexed
            modelBuilder.Entity<Contact>(e =>
            {
                e.Property(c => c.UserId).HasMaxLength(450);
                e.Property(c => c.Email).HasMaxLength(256);
            });

            // Invoices — constrain UserId, InvoiceCode, CustomerCode
            modelBuilder.Entity<Invoice>(e =>
            {
                e.Property(i => i.UserId).HasMaxLength(450);
                e.Property(i => i.InvoiceCode).HasMaxLength(128);
                e.Property(i => i.CustomerCode).HasMaxLength(128);
                e.Property(i => i.ExternalInvoiceId).HasMaxLength(256);
            });

            // KPIs — constrain UserId
            modelBuilder.Entity<KPI>(e =>
            {
                e.Property(k => k.UserId).HasMaxLength(450);
            });

            // KpiPolicies — UserId
            modelBuilder.Entity<KpiPolicy>(e =>
            {
                e.Property(k => k.UserId).HasMaxLength(450);
            });

            // LeadLakes — constrain UserId & Email
            modelBuilder.Entity<LeadLake>(e =>
            {
                e.Property(l => l.UserId).HasMaxLength(450);
                e.Property(l => l.Email).HasMaxLength(256);
            });

            // Meetings — UserId
            modelBuilder.Entity<Meeting>(e =>
            {
                e.Property(m => m.UserId).HasMaxLength(450);
            });

            // BankAccounts — UserId
            modelBuilder.Entity<BankAccount>(e =>
            {
                e.Property(b => b.UserId).HasMaxLength(450);
            });

            // Payouts — UserId
            modelBuilder.Entity<Payout>(e =>
            {
                e.Property(p => p.UserId).HasMaxLength(450);
            });

            // Assessments — UserId
            modelBuilder.Entity<Assessment>(e =>
            {
                e.Property(a => a.UserId).HasMaxLength(450);
            });

            // AssessmentSubmissions — UserId
            modelBuilder.Entity<AssessmentSubmission>(e =>
            {
                e.Property(s => s.UserId).HasMaxLength(450);
            });

            // Inboxes — UserId
            modelBuilder.Entity<Inbox>(e =>
            {
                e.Property(i => i.UserId).HasMaxLength(450);
            });

            // Products — UserId
            modelBuilder.Entity<Product>(e =>
            {
                e.Property(p => p.UserId).HasMaxLength(450);
            });

            // ──────────────────────────────────────────────────────────
            // Performance indexes
            // ──────────────────────────────────────────────────────────

            // Contacts: filtered by UserId, Status, Email
            modelBuilder.Entity<Contact>(e =>
            {
                e.HasIndex(c => c.UserId).HasDatabaseName("IX_Contacts_UserId");
                e.HasIndex(c => c.Status).HasDatabaseName("IX_Contacts_Status");
                e.HasIndex(c => c.Email).HasDatabaseName("IX_Contacts_Email");
                e.HasIndex(c => new { c.UserId, c.Status }).HasDatabaseName("IX_Contacts_UserId_Status");
            });

            // Invoices: filtered by UserId, ClientId, ProductId
            modelBuilder.Entity<Invoice>(e =>
            {
                e.HasIndex(i => i.UserId).HasDatabaseName("IX_Invoices_UserId");
                e.HasIndex(i => i.ClientId).HasDatabaseName("IX_Invoices_ClientId");
                e.HasIndex(i => i.ProductId).HasDatabaseName("IX_Invoices_ProductId");
            });

            // KPIs: always queried by UserId + ProductId, latest first
            modelBuilder.Entity<KPI>(e =>
            {
                e.HasIndex(k => new { k.UserId, k.ProductId, k.CreatedAt })
                 .HasDatabaseName("IX_KPIs_UserId_ProductId_CreatedAt")
                 .IsDescending(false, false, true);
            });

            // Assessments: latest per ProductId
            modelBuilder.Entity<Assessment>(e =>
            {
                e.HasIndex(a => new { a.ProductId, a.UploadedAt })
                 .HasDatabaseName("IX_Assessments_ProductId_UploadedAt")
                 .IsDescending(false, true);
            });

            // AssessmentSubmissions: latest per UserId + ProductId
            modelBuilder.Entity<AssessmentSubmission>(e =>
            {
                e.HasIndex(s => new { s.UserId, s.ProductId, s.SubmittedAt })
                 .HasDatabaseName("IX_AssessmentSubmissions_UserId_ProductId_SubmittedAt")
                 .IsDescending(false, false, true);
            });

            // LeadLakes: filtered by UserId, Email
            modelBuilder.Entity<LeadLake>(e =>
            {
                e.HasIndex(l => l.UserId).HasDatabaseName("IX_LeadLakes_UserId");
                e.HasIndex(l => l.Email).HasDatabaseName("IX_LeadLakes_Email");
                e.HasIndex(l => new { l.UserId, l.Email }).HasDatabaseName("IX_LeadLakes_UserId_Email");
            });

            // Meetings: filtered by UserId
            modelBuilder.Entity<Meeting>(e =>
            {
                e.HasIndex(m => m.UserId).HasDatabaseName("IX_Meetings_UserId");
                e.HasIndex(m => m.ClientId).HasDatabaseName("IX_Meetings_ClientId");
                e.HasIndex(m => m.ProductId).HasDatabaseName("IX_Meetings_ProductId");
            });

            // BankAccounts: one per user, queried by UserId
            modelBuilder.Entity<BankAccount>(e =>
            {
                e.HasIndex(b => b.UserId)
                 .IsUnique()
                 .HasDatabaseName("IX_BankAccounts_UserId");
            });

            // Payouts: filtered by UserId
            modelBuilder.Entity<Payout>(e =>
            {
                e.HasIndex(p => p.UserId).HasDatabaseName("IX_Payouts_UserId");
                e.HasIndex(p => new { p.UserId, p.Month, p.Year }).HasDatabaseName("IX_Payouts_UserId_Month_Year");
            });

            // KpiPolicies: filtered by ProductId, IsActive
            modelBuilder.Entity<KpiPolicy>(e =>
            {
                e.HasIndex(k => k.ProductId).HasDatabaseName("IX_KpiPolicies_ProductId");
                e.HasIndex(k => k.IsActive).HasDatabaseName("IX_KpiPolicies_IsActive");
            });

            // ApplicationUser: ban check on every request
            modelBuilder.Entity<ApplicationUser>(e =>
            {
                e.HasIndex(u => u.IsBanned).HasDatabaseName("IX_AspNetUsers_IsBanned");
            });
        }

        public DbSet<KPI> KPIs { get; set; }

        public DbSet<KpiPolicy> KpiPolicies { get; set; }

        public DbSet<Contact> Contacts { get; set; }

        public DbSet<LeadLake> LeadLakes { get; set; }

        public DbSet<Meeting> Meetings { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<Invoice> Invoices { get; set; }

        public DbSet<BankAccount> BankAccounts { get; set; }

        public DbSet<Payout> Payouts { get; set; }

        public DbSet<Inbox> Inboxes { get; set; }

        public DbSet<AssessmentSubmission> AssessmentSubmissions { get; set; }

        public DbSet<Assessment> Assessments { get; set; }
    }
}
