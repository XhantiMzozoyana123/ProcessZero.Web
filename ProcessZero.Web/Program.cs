using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Infrastructure.Filters;
using ProcessZero.Infrastructure.Services;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load Configuration
builder.Configuration
       .SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// -----------------------------
// DATABASE
// -----------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// -----------------------------
// HANGFIRE (FIXED)
// -----------------------------
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1;
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        // Map JWT claims to .NET claim types for name and role so policies like "Admin" work
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});
// Infrastructure services are referenced above

// -----------------------------
// CORE SERVICES
// -----------------------------
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

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
// Background email worker and enqueuer: use Hangfire to send emails outside HTTP requests
builder.Services.AddScoped<IBackgroundEmailWorker, BackgroundEmailWorker>();
builder.Services.AddSingleton<IBackgroundEmailService, BackgroundEmailService>();

// CORS - Allow all origins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// -----------------------------
// HANGFIRE DASHBOARD (secured — admin only)
// -----------------------------
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Schedule recurring payroll job: generate monthly commissions report.
// Runs once per month (Cron.Monthly) via Hangfire and will invoke the
// registered IPayrollService.GenerateMonthlyCommissionsReportAsync implementation.
//RecurringJob.AddOrUpdate<ProcessZero.Application.Interfaces.IPayrollService>(
//    "generate-monthly-commissions",
//    svc => svc.GenerateMonthlyCommissionsReportAsync(),
//    Cron.Monthly());

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// -----------------------------
// MIDDLEWARE (Correct order for Azure)
// -----------------------------
app.UseCors();

// Only use HTTPS redirection in production if not behind a load balancer
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// -----------------------------
// ADMIN SEED (Background - non-blocking)
// -----------------------------
_ = Task.Run(async () =>
{
    await Task.Delay(2000);
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

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
    catch
    {
        // Silently fail - admin seed is not critical for startup
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
