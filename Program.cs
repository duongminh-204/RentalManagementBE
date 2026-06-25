using Backend.Authorization;
using Backend.Configuration;
using Backend.Data;
using Backend.Entities;
using Backend.Interfaces;
using Backend.Repositories;
using Backend.Repositories.Interfaces;
using Backend.Services;
using Backend.Services.Interfaces;
using Backend.Services.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

// Keep JWT claim types stable across .NET 8 (avoid "sub"/"role" vs ClaimTypes mismatch).
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// ====================== SERVICES ======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.ToString());
});
builder.Services.AddMemoryCache();

// ====================== COMFYUI (AI DECOR) ======================
builder.Services.Configure<ComfyUIOptions>(builder.Configuration.GetSection(ComfyUIOptions.SectionName));
builder.Services.Configure<BankWebhookOptions>(builder.Configuration.GetSection(BankWebhookOptions.SectionName));
builder.Services.AddHttpClient<IComfyUIService, ComfyUIService>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ComfyUIOptions>>().Value;
    var baseUrl = (options.BaseUrl ?? "http://127.0.0.1:8188").TrimEnd('/') + "/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(Math.Max(options.PollTimeoutSeconds + 30, 120));
});
builder.Services.AddScoped<IRoomDecorService, RoomDecorService>();

// ====================== CORS ======================
var defaultCorsOrigins = new[]
{
    "http://localhost:5173",
    "http://localhost:5174",
    "http://localhost:3000",
    "http://localhost:5000",
    "http://127.0.0.1:5173",
    "http://127.0.0.1:5175",
    "https://www.rentalmanagement.site",
    "https://rentalmanagement.site"
};

var configuredCorsOrigins = builder.Configuration["Cors:AllowedOrigins"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? Array.Empty<string>();

var allowedCorsOrigins = defaultCorsOrigins
    .Concat(configuredCorsOrigins)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedCorsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ====================== DB ======================
builder.Services.AddDbContext<RentalManagementDb>(options =>
{
    var connectionName = builder.Environment.IsProduction()
        ? "SomeeConnection"
        : "LocalConnection";

    var connectionString = builder.Configuration.GetConnectionString(connectionName)
        ?? builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException(
            $"Connection string '{connectionName}' is missing. " +
            "Copy appsettings.example.json to appsettings.json or set ConnectionStrings__LocalConnection.");

    options.UseSqlServer(connectionString);
});

// ====================== FILE STORAGE ======================
// Development / Docker → wwwroot/uploads (Local)
// Production + Provider=Azure → Azure Blob Storage
builder.Services.Configure<AzureStorageOptions>(builder.Configuration.GetSection("AzureStorage"));

var useAzureStorage = builder.Environment.IsProduction()
    && (builder.Configuration["FileStorage:Provider"] ?? "Local")
        .Equals("Azure", StringComparison.OrdinalIgnoreCase);

if (useAzureStorage)
    builder.Services.AddSingleton<IFileStorageService, AzureBlobStorageService>();
else
    builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// ====================== REPOSITORIES & SERVICES ======================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<JwtService>();

builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRoomService, RoomServices>();

builder.Services.AddScoped<IRoomManagementRepository, RoomManagementRepository>();
builder.Services.AddScoped<IRoomManagementService, RoomManagementService>();

builder.Services.AddScoped<IBuildingRepository, BuildingRepository>();
builder.Services.AddScoped<IBuildingService, BuildingService>();

builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ITenantService, TenantService>();

builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IContractService, ContractService>();

builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IVehicleService, VehicleService>();

builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddScoped<IExcelImportRepository, ExcelImportRepository>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();

builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

builder.Services.AddScoped<IAuthorizationHandler, OwnerRoleAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PackageFeatureAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ActiveUserAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, NotSuspendedAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ActiveSubscriptionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, JsonAuthorizationMiddlewareResultHandler>();

builder.Services.AddIdentityCore<User>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddRoles<Role>()
.AddEntityFrameworkStores<RentalManagementDb>()
.AddDefaultTokenProviders();

// ====================== JWT ======================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"];
        var jwtIssuer = builder.Configuration["Jwt:Issuer"];
        var jwtAudience = builder.Configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException("JWT Key is missing.");

        options.MapInboundClaims = false;

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
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole(RoleNames.Admin));

    options.AddPolicy(AuthorizationPolicies.OwnerOrAdmin, policy =>
        policy.RequireRole(RoleNames.Admin, RoleNames.Owner));

    options.AddPolicy(AuthorizationPolicies.OwnerOnly, policy =>
        policy.RequireRole(RoleNames.Owner));

    options.AddPolicy(AuthorizationPolicies.TenantOnly, policy =>
        policy.RequireRole(RoleNames.Tenant));

    options.AddPolicy(AuthorizationPolicies.ActiveUser, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new ActiveUserRequirement()));

    options.AddPolicy(AuthorizationPolicies.ActiveOwner, policy =>
        policy.AddRequirements(new OwnerRoleRequirement(), new ActiveUserRequirement(), new NotSuspendedRequirement()));

    options.AddPolicy(AuthorizationPolicies.ActiveOwnerSubscription, policy =>
        policy.AddRequirements(
                new OwnerRoleRequirement(),
                new ActiveUserRequirement(),
                new NotSuspendedRequirement(),
                new ActiveSubscriptionRequirement()));

    foreach (PackageFeature feature in Enum.GetValues<PackageFeature>())
    {
        options.AddPolicy(PackageCatalog.GetPolicyName(feature), policy =>
            policy.AddRequirements(new PackageFeatureRequirement(feature)));
    }
});

var app = builder.Build();

// ====================== MIGRATE + SEEDDATA ======================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RentalManagementDb>();

    try
    {

        await context.Database.MigrateAsync();
        Console.WriteLine("Database migrated successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Migration failed: " + ex.Message);
    }

    try
    {
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider, builder.Configuration);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Seed error: " + ex.Message);
    }
}

// ====================== MIDDLEWARE ======================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseCors("AllowFrontend");

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ====================== CREATE UPLOAD FOLDERS ======================
var webRoot = app.Environment.WebRootPath
              ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");

string[] folders =
{
    "uploads/cccd",
    "uploads/templates",
    "uploads/rooms",
    "uploads/vehicles",
    "uploads/avatars",
    "uploads/contracts"
};

foreach (var folder in folders)
{
    Directory.CreateDirectory(Path.Combine(webRoot, folder));
}

app.Run();
