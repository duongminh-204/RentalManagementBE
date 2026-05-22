using Backend.Data;
using Backend.Entities;
using Backend.Repositories;
using Backend.Repositories.Interfaces;
using Backend.Services;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using RoomRepositoryInterface = Backend.Interfaces.IRoomRepository;

var builder = WebApplication.CreateBuilder(args);

//  SERVICES 
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//  CORS 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5174",   
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:5000"
                  
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
  
// Database
builder.Services.AddDbContext<RentalManagementDb>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Repositories & Services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<RoomRepositoryInterface, RoomRepository>();
builder.Services.AddScoped<IRoomService, RoomServices>();
builder.Services.AddScoped<IRoomManagementRepository, RoomManagementRepository>();
builder.Services.AddScoped<IRoomManagementService, RoomManagementService>();
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

//  BUILD
var app = builder.Build();

// SEED DATA
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RentalManagementDb>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

    if (!await context.Roles.AnyAsync())
    {
        context.Roles.AddRange(
            new Role { Name = "Admin", Description = "Admin he thong" },
            new Role { Name = "Tenant", Description = "Nguoi thue tro" },
            new Role { Name = "Owner", Description = "Chu tro" }
        );
        await context.SaveChangesAsync();
        Console.WriteLine("Seed Roles thanh cong!");
    }

    var adminRole = await context.Roles.FirstOrDefaultAsync(role => role.Name == "Admin");
    if (adminRole == null)
    {
        adminRole = new Role { Name = "Admin", Description = "Admin he thong" };
        context.Roles.Add(adminRole);
        await context.SaveChangesAsync();
    }
    var adminEmail = "admin@rental.local";
    var adminPhone = "0900000001";

    var adminUser = await context.Users.FirstOrDefaultAsync(user => user.Email == adminEmail);
    if (adminUser == null)
    {
        adminUser = new User
        {
            RoleId = adminRole.RoleId,
            FullName = "Quản trị hệ thống",
            Email = adminEmail,
            PhoneNumber = adminPhone,
            IsActive = true,
            Address = "Tài khoản seed sẵn cho admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin@123");

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
        Console.WriteLine("Seed Admin account thanh cong!");
    }
}


//MIDDLEWARE 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}


app.UseCors("AllowFrontend");

var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "cccd"));
Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "templates"));
Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "rooms"));
Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "vehicles"));
app.UseStaticFiles();

// app.UseHttpsRedirection();   
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
