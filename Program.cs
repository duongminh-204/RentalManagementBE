using Backend.Data;
using Backend.Entities;
using Backend.Interfaces;
using Backend.Repositories;
using Backend.Repositories.Interfaces;
using Backend.Services;
using Backend.Services.Interfaces;
using Backend.Services.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ====================== SERVICES ======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.ToString());
});
builder.Services.AddMemoryCache();

// ====================== CORS ======================
var defaultCorsOrigins = new[]
{
    "http://localhost:5173",
    "http://localhost:5174",
    "http://localhost:3000",
    "http://localhost:5000",
    "http://127.0.0.1:5173",
    "http://127.0.0.1:5175"
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

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// ====================== JWT ======================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"];
        var jwtIssuer = builder.Configuration["Jwt:Issuer"];
        var jwtAudience = builder.Configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException("JWT Key is missing.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

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
        // SEED DATA
        if (!context.Roles.Any())
        {
            context.Roles.AddRange(
                new Role { Name = "Admin", Description = "Admin hệ thống" },
                new Role { Name = "Tenant", Description = "Người Thuê Trọ" },
                new Role { Name = "Owner", Description = "Chủ Trọ" }
            );

            await context.SaveChangesAsync();
            Console.WriteLine("Seed roles successfully.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Seed error: " + ex.Message);
    }

    if (builder.Configuration.GetValue<bool>("SeedCatalogs"))
    {
    try
    {
        // SEED DANH MỤC THIẾT BỊ (DeviceCatalog)
        if (!context.DeviceCatalogs.Any())
        {
            context.DeviceCatalogs.AddRange(
                new DeviceCatalog { Name = "Máy lạnh", Icon = "AirVent" },
                new DeviceCatalog { Name = "Tủ lạnh", Icon = "Refrigerator" },
                new DeviceCatalog { Name = "Máy giặt", Icon = "WashingMachine" },
                new DeviceCatalog { Name = "Tivi", Icon = "Tv" },
                new DeviceCatalog { Name = "Lò vi sóng", Icon = "Microwave" },
                new DeviceCatalog { Name = "Quạt trần", Icon = "Fan" },
                new DeviceCatalog { Name = "Đèn LED", Icon = "Lightbulb" },
                new DeviceCatalog { Name = "Giường ngủ", Icon = "BedDouble" },
                new DeviceCatalog { Name = "Ghế sofa", Icon = "Sofa" },
                new DeviceCatalog { Name = "Tủ quần áo", Icon = "Shirt" },
                new DeviceCatalog { Name = "Bình nóng lạnh", Icon = "Flame" },
                new DeviceCatalog { Name = "Camera an ninh", Icon = "Cctv" },
                new DeviceCatalog { Name = "Bàn làm việc", Icon = "Table" },
                new DeviceCatalog { Name = "Khóa cửa thông minh", Icon = "Lock" }
            );

            await context.SaveChangesAsync();
            Console.WriteLine("Seed device catalog successfully.");
        }

        // SEED DANH MỤC DỊCH VỤ (Service)
        if (!context.Services.Any())
        {
            context.Services.AddRange(
                new Service { ServiceName = "Internet cáp quang", Icon = "Wifi", UnitPrice = 150000m, BillingCycle = "Monthly", Unit = "tháng" },
                new Service { ServiceName = "Dọn vệ sinh", Icon = "Sparkles", UnitPrice = 50000m, BillingCycle = "Monthly", Unit = "tháng" },
                new Service { ServiceName = "Giữ xe", Icon = "Car", UnitPrice = 100000m, BillingCycle = "Monthly", Unit = "xe/tháng" },
                new Service { ServiceName = "Giặt ủi", Icon = "WashingMachine", UnitPrice = 20000m, BillingCycle = "Monthly", Unit = "kg" },
                new Service { ServiceName = "Nước uống", Icon = "Droplet", UnitPrice = 12000m, BillingCycle = "Monthly", Unit = "bình" },
                new Service { ServiceName = "Bảo vệ 24/7", Icon = "ShieldCheck", UnitPrice = 0m, BillingCycle = "Monthly", Unit = "tháng" }
            );

            await context.SaveChangesAsync();
            Console.WriteLine("Seed service catalog successfully.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Seed catalog error: " + ex.Message);
    }
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
