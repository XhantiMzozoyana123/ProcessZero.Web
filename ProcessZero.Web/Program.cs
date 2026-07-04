using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Application.Options;
using ProcessZero.Domain;
using ProcessZero.Infrastructure.Filters;
using ProcessZero.Infrastructure.Services;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// CONFIGURATION
// -----------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// -----------------------------
// DATABASE (EF CORE) & FACTORY
// -----------------------------
// We use AddDbContextFactory which registers the factory and options as SINGLETON.
// This is required for long-running background jobs.
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// We then register the DbContext as SCOPED by resolving it from the factory.
// This allows Identity and Scoped services to work without scope-mismatch errors.
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

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
    options.WorkerCount = Environment.ProcessorCount;
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
// JWT AUTH
// -----------------------------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Missing Jwt:Key");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Missing Jwt:Issuer");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Missing Jwt:Audience");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JwtBearer";
    options.DefaultChallengeScheme = "JwtBearer";
    options.DefaultSignInScheme = "Identity.Application";
    options.DefaultSignOutScheme = "Identity.Application";
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

    // Suppress the redirect challenge for API requests.
    // Without this, Identity's cookie middleware issues a 302 redirect
    // to /Account/Login when an API call lacks a valid JWT token.
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnChallenge = context =>
        {
            // Only suppress the default challenge behavior for API routes
            // (requests that expect JSON responses).
            if (context.Request.Headers.Accept.Contains("application/json"))
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                var body = System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = "Unauthorized",
                    message = "A valid JWT token is required."
                });
                return context.Response.WriteAsync(body);
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// -----------------------------
// CORE SERVICES
// -----------------------------
builder.Services.Configure<GoogleOAuthOptions>(
    builder.Configuration.GetSection("GoogleOAuth"));

builder.Services.Configure<CalOptions>(
    builder.Configuration.GetSection("CalOptions"));

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// -----------------------------
// APPLICATION SERVICES
// -----------------------------
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
builder.Services.AddScoped<IEmailBlasterService, EmailBlasterService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<ISurveyService, SurveyService>();
builder.Services.AddScoped<IInboxService, InboxService>();
builder.Services.AddScoped<IGoogleOAuthService, GoogleOAuthService>();
builder.Services.AddScoped<IGmailService, GmailService>();

// -----------------------------
// RELAY ENGINE
// -----------------------------
builder.Services.AddScoped<IRelayCampaignService, RelayCampaignService>();
builder.Services.AddScoped<IRelayLeadService, RelayLeadService>();
builder.Services.AddScoped<IRelayInboxService, RelayInboxService>();
builder.Services.AddScoped<IRelayService, RelayService>();
builder.Services.AddScoped<IRelaySequenceService, RelaySequenceService>();
builder.Services.AddScoped<IRelayInboxRotationService, RelayInboxRotationService>();
builder.Services.AddScoped<IRelayEmailTrackingService, RelayEmailTrackingService>();
builder.Services.AddScoped<IRelayA_BTestingService, RelayA_BTestingService>();
builder.Services.AddScoped<IRelayEmailSenderService, RelayEmailSenderService>();

// -----------------------------
// CAL.COM INTEGRATION
// -----------------------------
builder.Services.AddHttpClient<ICalService, CalService>();

// -----------------------------
// LLM / AI
// -----------------------------
builder.Services.AddHttpClient<ILLMService, LLMService>();
builder.Services.AddScoped<IAIExtractorService, AIExtractorService>();

// -----------------------------
// SUPPORT SERVICES
// -----------------------------
builder.Services.AddScoped<IImportStatusService, InMemoryImportStatusService>();
builder.Services.AddScoped<IWebinarService, WebinarService>();
builder.Services.AddScoped<ImportProcessor>();
builder.Services.AddScoped<IExtractService, ExtractService>();

// -----------------------------
// BACKGROUND SERVICES
// -----------------------------
builder.Services.AddScoped<IBackgroundEmailWorker, BackgroundEmailWorker>();
builder.Services.AddSingleton<IBackgroundEmailService, BackgroundEmailService>();
builder.Services.AddScoped<RelayCampaignBackgroundService>();

// -----------------------------
// CORS
// -----------------------------
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod());
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// -----------------------------
// PIPELINE
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

// -----------------------------
// HANGFIRE DASHBOARD
// -----------------------------
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// -----------------------------
// KUBERNETES ROUND-ROBIN TEST
// -----------------------------
app.MapGet("/whoami", () =>
{
    var podName = Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown";
    return Results.Ok(new
    {
        Pod = podName,
        Time = DateTime.UtcNow
    });
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// -----------------------------
// STARTUP INIT
// -----------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await SeedAdminUser(userManager, roleManager);

        var scheduler = services.GetRequiredService<RelayCampaignBackgroundService>();
        scheduler.Start();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Startup initialization failed");
    }
}

app.Run();

// -----------------------------
// SEED ADMIN
// -----------------------------
async Task SeedAdminUser(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager)
{
    var email = "admin@processzero.xyz";
    var password = "StrongP@ssword123";

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    var user = await userManager.FindByEmailAsync(email);

    if (user == null)
    {
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, "Admin");
    }
    else if (!await userManager.IsInRoleAsync(user, "Admin"))
    {
        await userManager.AddToRoleAsync(user, "Admin");
    }
}