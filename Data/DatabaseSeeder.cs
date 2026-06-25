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
    private const string DemoMarkerEmail = "owner1@demo.com";
    private const string DemoPassword = "Demo@123";

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

            if (configuration.GetValue("SeedDemoData", true))
                await SeedDemoDataAsync(context, userManager, logger);
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
        var existingPackages = await context.Packages.ToListAsync();
        var catalogNames = PackageCatalog.All
            .Select(d => d.PackageName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in PackageCatalog.All)
        {
            var package = existingPackages.FirstOrDefault(p =>
                string.Equals(p.PackageName, definition.PackageName, StringComparison.OrdinalIgnoreCase));

            if (package == null)
            {
                context.Packages.Add(new Package
                {
                    PackageName = definition.PackageName,
                    Price = definition.Price,
                    MaxRooms = definition.MaxRooms,
                    Description = definition.Description,
                    IsEnabled = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                continue;
            }

            package.PackageName = definition.PackageName;
            package.Price = definition.Price;
            package.MaxRooms = definition.MaxRooms;
            package.Description = definition.Description;
            package.IsEnabled = true;
            package.UpdatedAt = now;
        }

        foreach (var package in existingPackages)
        {
            if (!catalogNames.Contains(package.PackageName))
                package.IsEnabled = false;
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Synced package catalog (Starter, PRO, PREMIUM).");
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

    private static async Task SeedDemoDataAsync(RentalManagementDb context, UserManager<User> userManager, ILogger logger)
    {
        if (await context.Users.AnyAsync(u => u.Email == DemoMarkerEmail)) return;

        var now = DateTime.Now;
        var starter = await context.Packages.FirstAsync(p => p.PackageName == PackageCatalog.Starter);
        var pro = await context.Packages.FirstAsync(p => p.PackageName == PackageCatalog.Pro);
        var premium = await context.Packages.FirstAsync(p => p.PackageName == PackageCatalog.Premium);

        var owners = new List<User>();
        foreach (var (fullName, email, phone, createdAt, isActive, isSuspended) in new (string, string, string, DateTime, bool, bool)[]
        {
            ("Nguyễn Văn An", "owner1@demo.com", "0901000001", now.AddMonths(-8), true, false),
            ("Trần Thị Bình", "owner2@demo.com", "0901000002", now.AddMonths(-6), true, false),
            ("Lê Minh Cường", "owner3@demo.com", "0901000003", now.AddMonths(-4), true, true),
            ("Phạm Thu Dung", "owner4@demo.com", "0901000004", now.AddMonths(-2), true, false),
            ("Hoàng Quốc Em", "owner5@demo.com", "0901000005", now.AddMonths(-1), false, false),
            ("Võ Thị Phương", "owner6@demo.com", "0901000006", now.AddDays(-15), true, false),
        })
        {
            var user = await CreateDemoUserAsync(userManager, fullName, email, phone, RoleNames.Owner, createdAt, isActive, isSuspended);
            owners.Add(user);
        }

        foreach (var (fullName, email, phone, createdAt) in new (string, string, string, DateTime)[]
        {
            ("Nguyễn Khách Một", "tenant1@demo.com", "0902000001", now.AddMonths(-3)),
            ("Trần Khách Hai", "tenant2@demo.com", "0902000002", now.AddMonths(-1)),
        })
        {
            await CreateDemoUserAsync(userManager, fullName, email, phone, RoleNames.Tenant, createdAt);
        }

        var owner1 = owners[0];
        var owner2 = owners[1];
        var owner3 = owners[2];
        var owner4 = owners[3];
        var owner6 = owners[5];

        var building1 = new Building
        {
            UserId = owner1.Id,
            BuildingName = "Nhà trọ An Bình",
            Address = "123 Nguyễn Văn Cừ, Q.5, TP.HCM",
            Latitude = 10.755,
            Longitude = 106.660,
            CreatedAt = now.AddMonths(-7)
        };
        var building2 = new Building
        {
            UserId = owner1.Id,
            BuildingName = "KTX An Phú",
            Address = "45 Lê Văn Sỹ, Q.3, TP.HCM",
            Latitude = 10.786,
            Longitude = 106.688,
            CreatedAt = now.AddMonths(-5)
        };
        var building3 = new Building
        {
            UserId = owner2.Id,
            BuildingName = "Trọ Bình Yên",
            Address = "78 Cách Mạng Tháng 8, Q.10, TP.HCM",
            CreatedAt = now.AddMonths(-5)
        };
        var building4 = new Building
        {
            UserId = owner4.Id,
            BuildingName = "Phòng trọ Dung House",
            Address = "12 Võ Văn Tần, Q.3, TP.HCM",
            CreatedAt = now.AddMonths(-2)
        };
        var building5 = new Building
        {
            UserId = owner6.Id,
            BuildingName = "Trọ Phương Linh",
            Address = "90 Hoàng Hoa Thám, Q. Bình Thạnh, TP.HCM",
            CreatedAt = now.AddDays(-10)
        };

        context.Buildings.AddRange(building1, building2, building3, building4, building5);
        await context.SaveChangesAsync();

        var rooms = new List<Room>();
        rooms.AddRange(CreateRooms(building1.BuildingId, "A", 5, 3_500_000m, new[] { "Occupied", "Occupied", "Available", "Occupied", "Available" }));
        rooms.AddRange(CreateRooms(building2.BuildingId, "P", 4, 2_800_000m, new[] { "Occupied", "Occupied", "Occupied", "Available" }));
        rooms.AddRange(CreateRooms(building3.BuildingId, "B", 3, 3_000_000m, new[] { "Occupied", "Available", "Available" }));
        rooms.AddRange(CreateRooms(building4.BuildingId, "D", 2, 4_200_000m, new[] { "Occupied", "Occupied" }));
        rooms.AddRange(CreateRooms(building5.BuildingId, "PL", 3, 3_200_000m, new[] { "Occupied", "Available", "Occupied" }));

        context.Rooms.AddRange(rooms);
        await context.SaveChangesAsync();

        var renters = new List<Tenant>
        {
            CreateRenter("Lê Văn Hùng", "0903000001", "hung.le@mail.com", "079123456789"),
            CreateRenter("Phạm Thị Mai", "0903000002", "mai.pham@mail.com", "079234567890"),
            CreateRenter("Đỗ Quang Minh", "0903000003", "minh.do@mail.com", "079345678901"),
            CreateRenter("Bùi Thanh Tâm", "0903000004", "tam.bui@mail.com", "079456789012"),
            CreateRenter("Ngô Thị Lan", "0903000005", "lan.ngo@mail.com", "079567890123"),
            CreateRenter("Vũ Đức Thắng", "0903000006", "thang.vu@mail.com", "079678901234"),
            CreateRenter("Hồ Ngọc Trâm", "0903000007", "tram.ho@mail.com", "079789012345"),
            CreateRenter("Dương Văn Kiệt", "0903000008", "kiet.duong@mail.com", "079890123456"),
            CreateRenter("Lý Thị Hoa", "0903000009", "hoa.ly@mail.com", "079901234567"),
            CreateRenter("Trịnh Minh Quân", "0903000010", "quan.trinh@mail.com", "079012345678"),
        };

        context.Tenants.AddRange(renters);
        await context.SaveChangesAsync();

        var subscriptions = new List<Subscription>
        {
            CreateSubscription(owner1.Id, pro.PackageId, now.AddMonths(-6), now.AddMonths(6), "Active"),
            CreateSubscription(owner2.Id, starter.PackageId, now.AddMonths(-4), now.AddMonths(-1), "Expired"),
            CreateSubscription(owner3.Id, pro.PackageId, now.AddMonths(-3), now.AddMonths(3), "Suspended"),
            CreateSubscription(owner4.Id, starter.PackageId, now.AddMonths(-1), now.AddDays(5), "Active"),
            CreateSubscription(owner6.Id, premium.PackageId, now.AddDays(-10), now.AddMonths(2), "Active"),
        };

        context.Subscriptions.AddRange(subscriptions);
        await context.SaveChangesAsync();

        var payments = new List<SubscriptionPayment>();
        var paymentMethods = new[] { "BankTransfer", "MoMo", "VNPay", "Renewal" };

        foreach (var sub in subscriptions.Where(s => s.Status is "Active" or "Expired"))
        {
            var package = sub.PackageId == pro.PackageId ? pro
                : sub.PackageId == premium.PackageId ? premium
                : starter;

            for (var i = 5; i >= 0; i--)
            {
                var payDate = now.AddMonths(-i).AddDays(Random.Shared.Next(1, 20));
                if (payDate < sub.StartDate) continue;
                if (sub.Status == "Expired" && payDate > sub.EndDate.AddMonths(1)) continue;

                payments.Add(new SubscriptionPayment
                {
                    OwnerUserId = sub.OwnerUserId,
                    SubscriptionId = sub.SubscriptionId,
                    Amount = package.Price,
                    PaymentMethod = paymentMethods[i % paymentMethods.Length],
                    PaymentDate = payDate,
                    Status = "Success"
                });
            }
        }

        context.SubscriptionPayments.AddRange(payments);
        await context.SaveChangesAsync();

        var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@rentalmanagement.site");
        var auditLogs = new List<AuditLog>
        {
            new() { UserId = admin?.Id, Action = "Create", Entity = "Package", EntityId = starter.PackageId, IPAddress = "127.0.0.1", Timestamp = now.AddDays(-30), Details = "Seed Starter package" },
            new() { UserId = admin?.Id, Action = "Create", Entity = "Owner", EntityId = owner1.Id, IPAddress = "127.0.0.1", Timestamp = now.AddMonths(-8), Details = "Seed demo owner" },
            new() { UserId = owner1.Id, Action = "Login", Entity = "User", EntityId = owner1.Id, IPAddress = "192.168.1.10", Timestamp = now.AddDays(-1), Details = owner1.Email },
            new() { UserId = admin?.Id, Action = "Subscription", Entity = "Subscription", EntityId = subscriptions[0].SubscriptionId, IPAddress = "127.0.0.1", Timestamp = now.AddMonths(-6), Details = "Activated Pro" },
            new() { UserId = admin?.Id, Action = "Payment", Entity = "SubscriptionPayment", IPAddress = "127.0.0.1", Timestamp = now.AddDays(-5), Details = "Seed payment" },
            new() { UserId = admin?.Id, Action = "Update", Entity = "Owner", EntityId = owner3.Id, IPAddress = "127.0.0.1", Timestamp = now.AddDays(-10), Details = "Suspended" },
        };

        context.AuditLogs.AddRange(auditLogs);
        await context.SaveChangesAsync();

        logger.LogInformation("Seed demo data successfully.");
        Console.WriteLine("=== DEMO ACCOUNTS (password: Demo@123) ===");
        Console.WriteLine("Admin:  admin@rentalmanagement.site / Admin@123");
        Console.WriteLine("Owners: owner1@demo.com .. owner6@demo.com");
        Console.WriteLine("Tenants: tenant1@demo.com, tenant2@demo.com");
    }

    private static async Task<User> CreateDemoUserAsync(
        UserManager<User> userManager,
        string fullName,
        string email,
        string phone,
        string roleName,
        DateTime createdAt,
        bool isActive = true,
        bool isSuspended = false)
    {
        var user = new User
        {
            UserName = email,
            FullName = fullName,
            Email = email,
            PhoneNumber = phone,
            IsActive = isActive,
            IsSuspended = isSuspended,
            CreatedAt = createdAt,
            UpdatedAt = DateTime.Now
        };

        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Seed user {email} failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        user.VisiblePassword = DemoPassword;
        await userManager.UpdateAsync(user);
        await userManager.AddToRoleAsync(user, roleName);
        return user;
    }

    private static IEnumerable<Room> CreateRooms(int buildingId, string prefix, int count, decimal price, string[] statuses)
    {
        for (var i = 0; i < count; i++)
        {
            yield return new Room
            {
                BuildingId = buildingId,
                RoomName = $"{prefix}{101 + i}",
                Status = statuses[i],
                Price = price,
                ElectricPrice = 3_500m,
                WaterPrice = 20_000m,
                Area = 18 + i * 2,
                MaxPeople = 2,
                CreatedAt = DateTime.Now.AddMonths(-3)
            };
        }
    }

    private static Tenant CreateRenter(string fullName, string phone, string email, string cccd)
    {
        return new Tenant
        {
            FullName = fullName,
            PhoneNumber = phone,
            Email = email,
            CCCD = cccd,
            Gender = "Nam",
            IsActive = true,
            CreatedAt = DateTime.Now.AddMonths(-2),
            UpdatedAt = DateTime.Now
        };
    }

    private static Subscription CreateSubscription(int ownerUserId, int packageId, DateTime start, DateTime end, string status)
    {
        return new Subscription
        {
            OwnerUserId = ownerUserId,
            PackageId = packageId,
            StartDate = start,
            EndDate = end,
            Status = status,
            CreatedAt = start,
            UpdatedAt = DateTime.Now
        };
    }
}
