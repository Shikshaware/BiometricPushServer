using System.Text;
using BiometricPushServer.Data;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository;
using BiometricPushServer.Repository.Interfaces;
using BiometricPushServer.Service;
using BiometricPushServer.Service.Interfaces;
using BiometricPushServer.Web.Filters;
using BiometricPushServer.Web.Hubs;
using BiometricPushServer.Web.Jobs;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// ── Serilog ──────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/biometric-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

var config = builder.Configuration;
var configuredUrls =
    config["urls"] ??
    config["ASPNETCORE_URLS"] ??
    Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
var hasConfiguredKestrelEndpoints = config.GetSection("Kestrel:Endpoints").Exists();
var hasValidConfiguredUrls = false;

if (!string.IsNullOrWhiteSpace(configuredUrls))
{
    foreach (var url in configuredUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUrl))
        {
            continue;
        }

        if (string.Equals(parsedUrl.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(parsedUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            hasValidConfiguredUrls = true;
            break;
        }
    }
}

var defaultDeviceUrl =
    config["DeviceCompatibility:DefaultHttpUrl"] ??
    "http://localhost:5000";
var allowHttpIClock = config.GetValue("DeviceCompatibility:AllowHttpIClock", false);

if (!hasValidConfiguredUrls && !hasConfiguredKestrelEndpoints)
{
    // Keep the legacy device port when the host has not explicitly configured bindings.
    builder.WebHost.UseUrls(
        Uri.TryCreate(defaultDeviceUrl, UriKind.Absolute, out _)
            ? defaultDeviceUrl
            : "http://localhost:5000");
}

var connStr = config.GetConnectionString("Default")
              ?? "Server=localhost;Database=BiometricPushServer;Trusted_Connection=True;";

// ── EF Core ───────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<BiometricDbContext>(opts =>
    opts.UseSqlServer(connStr));

// ── Repository / UoW ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPasswordHasher<BioPortalUser>, PasswordHasher<BioPortalUser>>();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ICommandService, CommandService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<ILocationService, LocationService>();

// ── MVC + Razor Views ─────────────────────────────────────────────────────────
// AutoValidateAntiforgeryToken applies CSRF validation to all unsafe-verb MVC actions globally;
// API and device endpoints opt out via [IgnoreAntiforgeryToken] on their controllers.
builder.Services.AddControllersWithViews(opts =>
{
    opts.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
})
.AddNewtonsoftJson();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Authentication (Cookie + JWT) ─────────────────────────────────────────────
var jwtSecret = config["Auth:JwtSecret"] ?? "BiometricPushServerDefaultSecretKey_ChangeInProd";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(opts =>
{
    opts.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, opts =>
{
    opts.LoginPath = "/Account/Login";
    opts.LogoutPath = "/Account/Logout";
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opts =>
{
    // Only skip HTTPS metadata requirement in Development (e.g. local HTTP testing)
    opts.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    opts.SaveToken = true;
    opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "BiometricPushServer",
        ValidateAudience = true,
        ValidAudience = "BiometricPushServer",
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ── Hangfire ──────────────────────────────────────────────────────────────────
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connStr, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BiometricPushServer API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (allowHttpIClock)
{
    // IClock devices are commonly configured for plain HTTP and may not follow HTTPS redirects.
    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/iclock", StringComparison.OrdinalIgnoreCase),
        branch => branch.UseHttpsRedirection());
}
else
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseSerilogRequestLogging();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// SignalR hub
app.MapHub<AttendanceHub>("/hubs/attendance");

// Hangfire dashboard — require authenticated admin user
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAdminAuthFilter() }
});

// MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");
// Auto-migrate on startup (development only; use explicit migration in production)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BiometricDbContext>();
    db.Database.Migrate();
}

// Schedule recurring Hangfire jobs
BiometricBackgroundJobs.ScheduleAll();

app.Run();
