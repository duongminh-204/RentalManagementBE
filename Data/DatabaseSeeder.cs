using Backend.Authorization;
using Backend.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Backend.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var context = services.GetRequiredService<RentalManagementDb>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<Role>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

        try
        {
            await SeedRolesAsync(roleManager, logger);
            await SyncPackageCatalogAsync(context, logger);
            await SeedAdminUserAsync(userManager, logger, configuration);

            if (configuration.GetValue("SeedCatalogs", true))
                await SeedCatalogsAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database seed failed.");
            Console.WriteLine("Seed error: " + ex.Message);
        }
    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager, ILogger logger)
    {
        var roles = new (string Name, string Description)[]
        {
            (RoleNames.Admin, "Admin hệ thống"),
            (RoleNames.Tenant, "Người Thuê Trọ"),
            (RoleNames.Owner, "Chủ Trọ")
        };

        foreach (var (name, description) in roles)
        {
            if (await roleManager.RoleExistsAsync(name)) continue;
            await roleManager.CreateAsync(new Role { Name = name, Description = description });
        }

        logger.LogInformation("Seed roles successfully.");
    }

    private static async Task SyncPackageCatalogAsync(RentalManagementDb context, ILogger logger)
    {
        var now = DateTime.Now;
        var existingNames = await context.Packages
            .Select(p => p.PackageName)
            .ToListAsync();
        var existingSet = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = 0;

        foreach (var definition in PackageCatalog.All)
        {
            if (existingSet.Contains(definition.PackageName))
                continue;

            context.Packages.Add(new Package
            {
                PackageName = definition.PackageName,
                Price = definition.Price,
                MaxRooms = definition.MaxRooms,
                Description = definition.Description,
                RoomRange = definition.RoomRange,
                TargetAudience = definition.TargetAudience,
                IsRecommended = definition.Recommended,
                FeatureLines = string.Join('\n', definition.FeatureLines),
                IsEnabled = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} default packages (Starter, PRO, PREMIUM).", added);
        }

        await BackfillPackageDisplayFieldsAsync(context, logger);
    }

    private static async Task BackfillPackageDisplayFieldsAsync(RentalManagementDb context, ILogger logger)
    {
        var packages = await context.Packages
            .Where(p => p.FeatureLines == null || p.RoomRange == null)
            .ToListAsync();

        if (packages.Count == 0) return;

        var updated = 0;
        foreach (var package in packages)
        {
            var definition = PackageCatalog.Find(package.PackageName);
            if (definition == null) continue;

            package.RoomRange ??= definition.RoomRange;
            package.TargetAudience ??= definition.TargetAudience;
            package.IsRecommended = package.IsRecommended || definition.Recommended;
            package.FeatureLines ??= string.Join('\n', definition.FeatureLines);
            package.Description ??= definition.Description;
            updated++;
        }

        if (updated > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Backfilled display fields for {Count} packages.", updated);
        }
    }

    private static async Task SeedAdminUserAsync(
        UserManager<User> userManager,
        ILogger logger,
        IConfiguration configuration)
    {
        const string adminEmail = "admin@rentalmanagement.site";
        const string adminPassword = "Admin@123";

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new User
            {
                UserName = adminEmail,
                FullName = "System Admin",
                Email = adminEmail,
                PhoneNumber = "0900000000",
                IsActive = true,
                IsSuspended = false,
                CreatedAt = DateTime.Now.AddMonths(-12),
                UpdatedAt = DateTime.Now
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Seed admin user failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(admin, RoleNames.Admin);
            logger.LogInformation("Seed admin user: {Email} / {Password}", adminEmail, adminPassword);
            return;
        }

        if (!await userManager.IsInRoleAsync(admin, RoleNames.Admin))
            await userManager.AddToRoleAsync(admin, RoleNames.Admin);

        var ensurePassword = configuration.GetValue("EnsureSeedAdminPassword", false);
        if (ensurePassword && !await userManager.CheckPasswordAsync(admin, adminPassword))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(admin);
            var resetResult = await userManager.ResetPasswordAsync(admin, resetToken, adminPassword);
            if (!resetResult.Succeeded)
            {
                logger.LogError(
                    "Reset seed admin password failed: {Errors}",
                    string.Join(", ", resetResult.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogWarning("Reset seed admin password for {Email}", adminEmail);
        }
    }

    private static async Task SeedCatalogsAsync(RentalManagementDb context, ILogger logger)
    {
        if (!await context.DeviceCatalogs.AnyAsync())
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
            logger.LogInformation("Seed device catalog successfully.");
        }

        if (!await context.Services.AnyAsync())
        {
            context.Services.AddRange(
                new Service { ServiceName = "Internet cáp quang", Icon = "Wifi", UnitPrice = 150_000m, BillingCycle = "Monthly", Unit = "tháng" },
                new Service { ServiceName = "Dọn vệ sinh", Icon = "Sparkles", UnitPrice = 50_000m, BillingCycle = "Monthly", Unit = "tháng" },
                new Service { ServiceName = "Giữ xe", Icon = "Car", UnitPrice = 100_000m, BillingCycle = "Monthly", Unit = "xe/tháng" },
                new Service { ServiceName = "Giặt ủi", Icon = "WashingMachine", UnitPrice = 20_000m, BillingCycle = "Monthly", Unit = "kg" },
                new Service { ServiceName = "Nước uống", Icon = "Droplet", UnitPrice = 12_000m, BillingCycle = "Monthly", Unit = "bình" },
                new Service { ServiceName = "Bảo vệ 24/7", Icon = "ShieldCheck", UnitPrice = 0m, BillingCycle = "Monthly", Unit = "tháng" }
            );
            await context.SaveChangesAsync();
            logger.LogInformation("Seed service catalog successfully.");
        }
    }
}
