using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProcessZero.Application.Interfaces;
using ProcessZero.Application.Options;
using ProcessZero.Domain;
using ProcessZero.Infrastructure.Filters;
using ProcessZero.Infrastructure.Services;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// CONFIGURATION & DATABASE
// -----------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// -----------------------------
// HANGFIRE
// -----------------------------
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseStorage(new MySqlStorage(connectionString, new MySqlStorageOptions
          {
              TablesPrefix = "Hangfire"
          }));
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1; // Single worker for sequential processing if needed
});

// -----------------------------
// IDENTITY
// -----------------------------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// -----------------------------
// JWT AUTHENTICATION
// -----------------------------
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Missing Jwt:Issuer");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Missing Jwt:Audience");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JwtBearer";
    options.DefaultChallengeScheme = "JwtBearer";
})
.AddJwtBearer("JwtBearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// -----------------------------
// CORE & INFRASTRUCTURE SERVICES
// -----------------------------
builder.Services.Configure<GoogleOAuthOptions>(builder.Configuration.GetSection("GoogleOAuth"));
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IKpiService, KpiService>();
builder.Services.AddScoped<IKpiPolicyService, KpiPolicyService>();
builder.Services.AddScoped<IMeetingService, MeetingService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IBankAccountService, BankAccountService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<ILeadLakeService, LeadLakeService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<IInboxService, InboxService>();
builder.Services.AddScoped<IGoogleOAuthService, GoogleOAuthService>();
builder.Services.AddScoped<IGmailService, GmailService>();

// Relay Engine Services
builder.Services.AddScoped<IRelayCampaignService, RelayCampaignService>();
builder.Services.AddScoped<IRelayLeadService, RelayLeadService>();
builder.Services.AddScoped<IRelayInboxService, RelayInboxService>();
builder.Services.AddScoped<IRelayService, RelayService>();
builder.Services.AddScoped<IRelaySequenceService, RelaySequenceService>();
builder.Services.AddScoped<IRelayInboxRotationService, RelayInboxRotationService>();
builder.Services.AddScoped<IRelayEmailTrackingService, RelayEmailTrackingService>();
builder.Services.AddScoped<IRelayA_BTestingService, RelayA_BTestingService>();
builder.Services.AddScoped<IRelayEmailSenderService, RelayEmailSenderService>();

// LLM Service
builder.Services.AddHttpClient<ILLMService, LLMService>();
builder.Services.AddScoped<IAIExtractorService, AIExtractorService>();

// Support Services
builder.Services.AddScoped<IImportStatusService, InMemoryImportStatusService>();
builder.Services.AddScoped<IWebinarService, WebinarService>();
builder.Services.AddScoped<ImportProcessor>();
builder.Services.AddScoped<IExtractService, ExtractService>();

// Background Tasks
builder.Services.AddScoped<IBackgroundEmailWorker, BackgroundEmailWorker>();
builder.Services.AddSingleton<IBackgroundEmailService, BackgroundEmailService>();
builder.Services.AddScoped<RelayCampaignBackgroundService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// -----------------------------
// HTTP PIPELINE
// -----------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (Secured)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

// -----------------------------
// INITIALIZATION (Seeding & Recurring Jobs)
// -----------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // 1. Seed Admin User
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await SeedAdminUser(userManager, roleManager);

        // 2. Start Relay Scheduler (Register Hangfire Jobs)
        var scheduler = services.GetRequiredService<RelayCampaignBackgroundService>();
        scheduler.Start();

        // 3. Register Monthly Payroll Job (Example)
        // RecurringJob.AddOrUpdate<IPayrollService>(
        //    "generate-monthly-commissions",
        //    svc => svc.GenerateMonthlyCommissionsReportAsync(),
        //    Cron.Monthly());
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during startup initialization.");
    }
}

app.Run();

// -----------------------------
// HELPERS
// -----------------------------
async Task SeedAdminUser(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
{
    var email = "admin@processzero.xyz";
    var password = "StrongP@ssword123";

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded) await userManager.AddToRoleAsync(user, "Admin");
    }
    else if (!await userManager.IsInRoleAsync(user, "Admin"))
    {
        await userManager.AddToRoleAsync(user, "Admin");
    }
}