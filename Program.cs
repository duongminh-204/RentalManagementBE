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

// ====================== CONFIGURATION ======================
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// ====================== SERVICES ======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
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

// Database
builder.Services.AddDbContext<RentalManagementDb>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
    });
});

// Repositories & Services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRoomService, RoomServices>();
builder.Services.AddScoped<IRoomManagementRepository, RoomManagementRepository>();
builder.Services.AddScoped<IRoomManagementService, RoomManagementService>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IContractRepository, ContractRepository>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IVehicleService, VehicleService>();

// JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ====================== BUILD APP ======================
var app = builder.Build();

// ====================== SEED DATA ======================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RentalManagementDb>();
    try
    {
        int retries = 10;
        while (retries > 0)
        {
            try
            {
                await context.Database.MigrateAsync();
                break;
            }
            catch (Exception ex)
            {
                retries--;
                if (retries == 0) throw;
                Console.WriteLine($"Database not ready yet. Retrying in 5 seconds... ({ex.Message})");
                await Task.Delay(5000);
            }
        }

        if (!context.Roles.Any())
        {
            context.Roles.AddRange(
                new Role { Name = "Admin", Description = "Admin hệ thống" },
                new Role { Name = "Tenant", Description = "Người Thuê Trọ" },
                new Role { Name = "Owner", Description = "Chủ Trọ" }
            );
            await context.SaveChangesAsync();
            Console.WriteLine("✅ Seed Roles thành công!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Seed data error: {ex.Message}");
    }
}

// ====================== MIDDLEWARE ======================
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseCors("AllowFrontend");

// ====================== CREATE UPLOAD FOLDERS ======================
var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");

var uploadFolders = new[]
{
    Path.Combine(webRoot, "uploads", "cccd"),
    Path.Combine(webRoot, "uploads", "templates"),
    Path.Combine(webRoot, "uploads", "rooms"),
    Path.Combine(webRoot, "uploads", "vehicles")
};

foreach (var folder in uploadFolders)
{
    try
    {
        Directory.CreateDirectory(folder);
        Console.WriteLine($"✅ Created folder: {folder}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Warning: Cannot create directory {folder}: {ex.Message}");
    }
}

app.UseStaticFiles();
// app.UseHttpsRedirection(); 

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();