using Backend.Data;
using Backend.Entities;
using Backend.Interfaces;
using Backend.Repositories;
using Backend.Repositories.Interfaces;
using Backend.Services;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ====================== CONFIG ======================
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// ====================== SERVICES ======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ====================== CORS ======================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:3000",
                "http://localhost:5000",
                "http://127.0.0.1:5173"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ====================== DB ======================
builder.Services.AddDbContext<RentalManagementDb>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

    options.UseSqlServer(connectionString);
});

// ====================== REPOSITORIES & SERVICES ======================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
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
    "uploads/vehicles"
};

foreach (var folder in folders)
{
    Directory.CreateDirectory(Path.Combine(webRoot, folder));
}

app.Run();