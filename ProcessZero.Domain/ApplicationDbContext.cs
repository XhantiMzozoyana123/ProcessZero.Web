using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProcessZero.Domain.Entities;

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
            // Build the path to the ProcessZero.Web project
            var webProjectPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, "ProcessZero.Web");

            // Load the configuration from appsettings.json in the Web project
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(webProjectPath) // Set the base path to the Web directory
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Load appsettings.json
                .Build();

            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Choose the appropriate database provider here.
            // A pinned server version is used so design-time tooling (e.g.
            // `dotnet ef migrations add`) does not require a live MySQL
            // connection. Adjust the version to match your MySQL server.
            builder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));

            return builder.Options;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Workshop configurations removed

            // ──────────────────────────────────────────────────────────
            // Column length constraints (required for indexing nvarchar)
            // ──────────────────────────────────────────────────────────

            modelBuilder.Entity<Contact>(e =>
            {
                e.Property(c => c.UserId).HasMaxLength(450);
                e.Property(c => c.Email).HasMaxLength(256);
            });

            modelBuilder.Entity<Invoice>(e =>
            {
                e.Property(i => i.UserId).HasMaxLength(450);
                e.Property(i => i.InvoiceCode).HasMaxLength(128);
                e.Property(i => i.CustomerCode).HasMaxLength(128);
                e.Property(i => i.ExternalInvoiceId).HasMaxLength(256);
            });

            modelBuilder.Entity<KPI>(e =>
            {
                e.Property(k => k.UserId).HasMaxLength(450);
            });

            modelBuilder.Entity<KpiPolicy>(e =>
            {
                e.Property(k => k.UserId).HasMaxLength(450);
                e.Property(k => k.PolicyName).HasMaxLength(100);
            });

            modelBuilder.Entity<LeadLake>(e =>
            {
                e.Property(l => l.UserId).HasMaxLength(450);
                e.Property(l => l.Email).HasMaxLength(256);
            });

            modelBuilder.Entity<Meeting>(e => 
            {
                e.Property(m => m.UserId).HasMaxLength(450);
            });

            modelBuilder.Entity<BankAccount>(e =>
            {
                e.Property(b => b.UserId).HasMaxLength(450);
            });

            modelBuilder.Entity<Payout>(e =>
            {
                e.Property(p => p.UserId).HasMaxLength(450);
            });

            modelBuilder.Entity<Assessment>(e =>
            {
                e.Property(a => a.UserId).HasMaxLength(450);
            });

            modelBuilder.Entity<AssessmentSubmission>(e =>
            {
                e.Property(s => s.UserId).HasMaxLength(450);
            });

            modelBuilder.Entity<Survey>(e =>
            {
                e.Property(r => r.UserId).HasMaxLength(450);
                e.Property(r => r.Name).HasMaxLength(255).IsRequired();
                e.Property(r => r.Title).HasMaxLength(200);
                e.Property(r => r.Status).HasMaxLength(50).HasDefaultValue("Active");
            });

            modelBuilder.Entity<SurveyQuestion>(e =>
            {
                e.Property(r => r.UserId).HasMaxLength(450);
                e.Property(r => r.Text).HasMaxLength(500).IsRequired();
            });

            modelBuilder.Entity<SurveyQuestionOption>(e =>
            {
                e.Property(o => o.UserId).HasMaxLength(450);
                e.Property(o => o.Text).HasMaxLength(255).IsRequired();
            });

            modelBuilder.Entity<SurveyRespondent>(e =>
            {
                e.Property(r => r.UserId).HasMaxLength(450);
                e.Property(r => r.Email).HasMaxLength(256).IsRequired();
                e.Property(r => r.FirstName).HasMaxLength(100).IsRequired();
                e.Property(r => r.LastName).HasMaxLength(100).IsRequired();
                e.Property(r => r.Phone).HasMaxLength(20).IsRequired();
                e.Property(r => r.Company).HasMaxLength(255);
                e.Property(r => r.Job).HasMaxLength(100);
                e.Property(r => r.Industry).HasMaxLength(100);
            });

            modelBuilder.Entity<SurveyResponse>(e =>
            {
                e.Property(r => r.UserId).HasMaxLength(450);
            });

            modelBuilder.Entity<SurveyAnswer>(e =>
            {
                e.Property(r => r.AnswerText).HasMaxLength(2000);
            });

            modelBuilder.Entity<Inbox>(e =>
            {
                e.Property(i => i.UserId).HasMaxLength(450);
            });

            modelBuilder.Entity<Product>(e =>
            {
                e.Property(p => p.UserId).HasMaxLength(450);
            });

            // Performance indexes
            modelBuilder.Entity<Contact>(e =>
            {
                e.HasIndex(c => c.UserId).HasDatabaseName("IX_Contacts_UserId");
                e.HasIndex(c => c.Status).HasDatabaseName("IX_Contacts_Status");
                e.HasIndex(c => c.Email).HasDatabaseName("IX_Contacts_Email");
                e.HasIndex(c => new { c.UserId, c.Status }).HasDatabaseName("IX_Contacts_UserId_Status");
            });

            modelBuilder.Entity<Invoice>(e =>
            {
                e.HasIndex(i => i.UserId).HasDatabaseName("IX_Invoices_UserId");
                e.HasIndex(i => i.ClientId).HasDatabaseName("IX_Invoices_ClientId");
                e.HasIndex(i => i.ProductId).HasDatabaseName("IX_Invoices_ProductId");
            });

            modelBuilder.Entity<KPI>(e =>
            {
                e.HasIndex(k => new { k.UserId, k.ProductId, k.CreatedAt })
                 .HasDatabaseName("IX_KPIs_UserId_ProductId_CreatedAt")
                 .IsDescending(false, false, true);

                e.HasIndex(k => new { k.UserId, k.CreatedAt })
                 .HasDatabaseName("IX_KPIs_UserId_CreatedAt")
                 .IsDescending(false, true);
            });

            modelBuilder.Entity<Assessment>(e =>
            {
                e.HasIndex(a => new { a.ProductId, a.UploadedAt })
                 .HasDatabaseName("IX_Assessments_ProductId_UploadedAt")
                 .IsDescending(false, true);
            });

            modelBuilder.Entity<AssessmentSubmission>(e =>
            {
                e.HasIndex(s => new { s.UserId, s.ProductId, s.SubmittedAt })
                 .HasDatabaseName("IX_AssessmentSubmissions_UserId_ProductId_SubmittedAt")
                 .IsDescending(false, false, true);
            });

            modelBuilder.Entity<Survey>(e =>
            {
                e.HasIndex(r => r.Status).HasDatabaseName("IX_Surveys_Status");
                e.HasIndex(r => r.UploadedAt)
                 .HasDatabaseName("IX_Surveys_UploadedAt")
                 .IsDescending(true);
                e.HasIndex(r => new { r.UserId, r.Status })
                 .HasDatabaseName("IX_Surveys_UserId_Status");
            });

            modelBuilder.Entity<SurveyQuestion>(e =>
            {
                e.HasIndex(r => new { r.SurveyId, r.Order })
                 .HasDatabaseName("IX_SurveyQuestions_SurveyId_Order");

                e.HasOne(r => r.Survey)
                 .WithMany(s => s.Questions)
                 .HasForeignKey(r => r.SurveyId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SurveyRespondent>(e =>
            {
                e.HasIndex(r => r.UserId).HasDatabaseName("IX_SurveyRespondents_UserId");
                e.HasIndex(r => new { r.SurveyId, r.Email })
                 .HasDatabaseName("IX_SurveyRespondents_SurveyId_Email")
                 .IsUnique();
                e.HasIndex(r => r.SurveyId).HasDatabaseName("IX_SurveyRespondents_SurveyId");

                e.HasOne(r => r.Survey)
                 .WithMany(s => s.Respondents)
                 .HasForeignKey(r => r.SurveyId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SurveyResponse>(e =>
            {
                e.HasIndex(r => r.SurveyId).HasDatabaseName("IX_SurveyResponses_SurveyId");
                e.HasIndex(r => new { r.SurveyId, r.SubmittedAt })
                 .HasDatabaseName("IX_SurveyResponses_SurveyId_SubmittedAt")
                 .IsDescending(false, true);
                e.HasIndex(r => new { r.SurveyRespondentId, r.SubmittedAt })
                 .HasDatabaseName("IX_SurveyResponses_RespondentId_SubmittedAt")
                 .IsDescending(false, true);

                e.HasOne(r => r.Survey)
                 .WithMany(s => s.Responses)
                 .HasForeignKey(r => r.SurveyId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(r => r.Respondent)
                 .WithMany(s => s.Responses)
                 .HasForeignKey(r => r.SurveyRespondentId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SurveyAnswer>(e =>
            {
                e.HasIndex(r => new { r.SurveyResponseId, r.SurveyQuestionId })
                 .HasDatabaseName("IX_SurveyAnswers_ResponseId_QuestionId")
                 .IsUnique();

                e.HasOne(r => r.SurveyResponse)
                 .WithMany(sr => sr.Answers)
                 .HasForeignKey(r => r.SurveyResponseId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(r => r.SurveyQuestion)
                 .WithMany()
                 .HasForeignKey(r => r.SurveyQuestionId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SurveyQuestionOption>(e =>
            {
                e.HasIndex(o => new { o.SurveyQuestionId, o.Order })
                 .HasDatabaseName("IX_SurveyQuestionOptions_QuestionId_Order");

                e.HasOne(o => o.SurveyQuestion)
                 .WithMany(q => q.Options)
                 .HasForeignKey(o => o.SurveyQuestionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LeadLake>(e =>
            {
                e.HasIndex(l => l.UserId).HasDatabaseName("IX_LeadLakes_UserId");
                e.HasIndex(l => l.Email).HasDatabaseName("IX_LeadLakes_Email");
                e.HasIndex(l => new { l.UserId, l.Email }).HasDatabaseName("IX_LeadLakes_UserId_Email");
            });

            modelBuilder.Entity<Meeting>(e =>
            {
                e.HasIndex(m => m.UserId).HasDatabaseName("IX_Meetings_UserId");
                e.HasIndex(m => m.ClientId).HasDatabaseName("IX_Meetings_ClientId");
                e.HasIndex(m => m.ProductId).HasDatabaseName("IX_Meetings_ProductId");
            });

            modelBuilder.Entity<BankAccount>(e =>
            {
                e.HasIndex(b => b.UserId)
                 .IsUnique()
                 .HasDatabaseName("IX_BankAccounts_UserId");
            });

            modelBuilder.Entity<Payout>(e =>
            {
                e.HasIndex(p => p.UserId).HasDatabaseName("IX_Payouts_UserId");
                e.HasIndex(p => new { p.UserId, p.Month, p.Year }).HasDatabaseName("IX_Payouts_UserId_Month_Year");
            });

            modelBuilder.Entity<KpiPolicy>(e =>
            {
                e.HasIndex(k => k.ProductId).HasDatabaseName("IX_KpiPolicies_ProductId");
                e.HasIndex(k => k.IsActive).HasDatabaseName("IX_KpiPolicies_IsActive");
                e.HasIndex(k => k.EffectiveFrom).HasDatabaseName("IX_KpiPolicies_EffectiveFrom");
            });

            modelBuilder.Entity<ApplicationUser>(e =>
            {
                e.HasIndex(u => u.IsBanned).HasDatabaseName("IX_AspNetUsers_IsBanned");
            });

            // RELAY EMAIL SYSTEM
            modelBuilder.Entity<RelayCampaign>(e =>
            {
                e.HasIndex(x => x.IsActive);
            });

            modelBuilder.Entity<RelaySequence>(e =>
            {
                e.HasOne(x => x.RelayCampaign)
                    .WithMany(x => x.Sequences)
                    .HasForeignKey(x => x.RelayCampaignId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => x.RelayCampaignId);
            });

            modelBuilder.Entity<RelaySequenceStep>(e =>
            {
                e.HasOne(x => x.RelaySequence)
                    .WithMany(x => x.Steps)
                    .HasForeignKey(x => x.RelaySequenceId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => x.RelaySequenceId);
                e.HasIndex(x => x.StepOrder);
            });

            modelBuilder.Entity<RelayEmailVariant>(e =>
            {
                e.HasOne(x => x.SequenceStep)
                    .WithMany(x => x.Variants)
                    .HasForeignKey(x => x.SequenceStepId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => x.SequenceStepId);
            });

            modelBuilder.Entity<RelayCampaignInbox>(e =>
            {
                e.HasKey(x => new { x.RelayCampaignId, x.RelayInboxId });

                e.HasOne(x => x.RelayCampaign)
                    .WithMany(x => x.Inboxes)
                    .HasForeignKey(x => x.RelayCampaignId);

                e.HasOne(x => x.RelayInbox)
                    .WithMany(x => x.Campaigns)
                    .HasForeignKey(x => x.RelayInboxId);

                e.HasIndex(x => x.RelayCampaignId);
            });

            modelBuilder.Entity<RelayCampaignLead>(e =>
            {
                e.HasIndex(x => new { x.RelayCampaignId, x.RelayLeadId }).IsUnique();

                e.HasIndex(x => x.RelayCampaignId);
                e.HasIndex(x => x.RelayLeadId);
                e.HasIndex(x => x.Status);

                e.HasOne(x => x.RelayCampaign)
                    .WithMany(x => x.Leads)
                    .HasForeignKey(x => x.RelayCampaignId);

                e.HasOne(x => x.RelayLead)
                    .WithMany(x => x.Campaigns)
                    .HasForeignKey(x => x.RelayLeadId);
            });

            modelBuilder.Entity<RelayEmailActivity>(e =>
            {
                e.HasIndex(x => x.GmailMessageId).IsUnique();
                e.HasIndex(x => x.GmailThreadId);

                e.HasIndex(x => new { x.RelayLeadId, x.SentAt });
                e.HasIndex(x => new { x.RelayInboxId, x.SentAt });
                e.HasIndex(x => x.EmailVariantId);
                e.HasIndex(x => x.RelayCampaignId);

                e.HasOne(x => x.RelayCampaign)
                    .WithMany()
                    .HasForeignKey(x => x.RelayCampaignId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.RelayLead)
                    .WithMany(x => x.Activities)
                    .HasForeignKey(x => x.RelayLeadId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.RelayInbox)
                    .WithMany(x => x.Activities)
                    .HasForeignKey(x => x.RelayInboxId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.EmailVariant)
                    .WithMany(x => x.Activities)
                    .HasForeignKey(x => x.EmailVariantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RelayLead>(e =>
            {
                e.HasIndex(x => x.Email).IsUnique();
            });

            modelBuilder.Entity<RelayEmailAccount>(e =>
            {
                e.HasIndex(x => x.EmailAddress).IsUnique();
                e.HasIndex(x => new { x.IsActive, x.SentToday });
            });

            // Credit System Configurations
            modelBuilder.Entity<UserWallet>(e =>
            {
                e.Property(w => w.UserId).HasMaxLength(450);
                e.Property(w => w.SubscriptionId).HasMaxLength(256);
                e.Property(w => w.SubscriptionStatus).HasMaxLength(50);

                e.HasIndex(w => w.UserId)
                    .IsUnique()
                    .HasDatabaseName("IX_UserWallets_UserId");

                e.HasOne(w => w.User)
                    .WithMany()
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CreditTransaction>(e =>
            {
                // CreditTransaction is linked to a UserWallet (which has UserId), not directly to a user
                e.Ignore(t => t.UserId);

                e.Property(t => t.Description).HasMaxLength(500);
                e.Property(t => t.ReferenceId).HasMaxLength(256);
                e.Property(t => t.RelatedEntityType).HasMaxLength(100);

                e.HasIndex(t => t.UserWalletId)
                    .HasDatabaseName("IX_CreditTransactions_UserWalletId");
                e.HasIndex(t => t.TransactionDate)
                    .HasDatabaseName("IX_CreditTransactions_TransactionDate");
                e.HasIndex(t => new { t.UserWalletId, t.TransactionDate })
                    .HasDatabaseName("IX_CreditTransactions_UserWalletId_TransactionDate")
                    .IsDescending(false, true);

                e.HasOne(t => t.UserWallet)
                    .WithMany()
                    .HasForeignKey(t => t.UserWalletId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CreditPackage>(e =>
            {
                // Credit packages are global, not user-specific
                e.Ignore(p => p.UserId);

                e.Property(p => p.Name).HasMaxLength(100);
                e.Property(p => p.Description).HasMaxLength(500);
                e.Property(p => p.Currency).HasMaxLength(3);

                e.HasIndex(p => p.IsActive)
                    .HasDatabaseName("IX_CreditPackages_IsActive");
                e.HasIndex(p => p.SortOrder)
                    .HasDatabaseName("IX_CreditPackages_SortOrder");
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
        public DbSet<Survey> Surveys { get; set; }
        public DbSet<SurveyQuestion> SurveyQuestions { get; set; }
        public DbSet<SurveyResponse> SurveyResponses { get; set; }
        public DbSet<SurveyRespondent> SurveyRespondents { get; set; }
        public DbSet<SurveyAnswer> SurveyAnswers { get; set; }
        public DbSet<SurveyQuestionOption> SurveyQuestionOptions { get; set; }
        public DbSet<RelayEmailAccount> RelayEmailAccounts { get; set; }
        public DbSet<RelayEmailReply> RelayEmailReplies { get; set; }
        public DbSet<Webinar> Webinars { get; set; }
        public DbSet<RelayCampaign> RelayCampaigns { get; set; }
        public DbSet<RelaySequence> RelaySequences { get; set; }
        public DbSet<RelaySequenceStep> RelaySequenceSteps { get; set; }
        public DbSet<RelayEmailVariant> RelayEmailVariants { get; set; }
        public DbSet<RelayCampaignInbox> RelayCampaignInboxes { get; set; }
        public DbSet<RelayCampaignLead> RelayCampaignLeads { get; set; }
        public DbSet<RelayLead> RelayLeads { get; set; }
        public DbSet<RelayEmailActivity> RelayEmailActivities { get; set; }
        public DbSet<ScheduledSmsMessage> ScheduledSmsMessages { get; set; }
        public DbSet<ScheduledWhatsAppMessage> ScheduledWhatsAppMessages { get; set; }
        public DbSet<ScheduledFacebookMessage> ScheduledFacebookMessages { get; set; }
        public DbSet<ScheduledEmailMessage> ScheduledEmailMessages { get; set; }

        // Credit System Entities
        public DbSet<UserWallet> UserWallets { get; set; }
        public DbSet<CreditTransaction> CreditTransactions { get; set; }
        public DbSet<CreditPackage> CreditPackages { get; set; }

        // Session tracking for credit consumption
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<ConsumptionConfig> ConsumptionConfigs { get; set; }
    }
}