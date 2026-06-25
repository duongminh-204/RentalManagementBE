using Backend.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Backend.Data;

public class RentalManagementDb : IdentityDbContext<User, Role, int>
{
    public RentalManagementDb(DbContextOptions<RentalManagementDb> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Building> Buildings { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<RoomImage> RoomImages { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<RoomService> RoomServices { get; set; }
    public DbSet<DeviceCatalog> DeviceCatalogs { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<UtilityUsage> UtilityUsages { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceDetail> InvoiceDetails { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Package> Packages { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<SubscriptionPayment> SubscriptionPayments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureDecimalPrecision(modelBuilder);

        // =========================
        // Users (ASP.NET Core Identity)
        // =========================
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(x => x.Id).HasColumnName("UserId");

            entity.Property(x => x.FullName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(x => x.Email)
                  .HasMaxLength(256);

            entity.Property(x => x.NormalizedEmail)
                  .HasMaxLength(256);

            entity.Property(x => x.UserName)
                  .HasMaxLength(256);

            entity.Property(x => x.NormalizedUserName)
                  .HasMaxLength(256);

            entity.Property(x => x.PhoneNumber)
                  .HasMaxLength(20);

            entity.Property(x => x.CCCD)
                  .HasMaxLength(20);

            entity.Property(x => x.Address)
                  .HasMaxLength(500);

            entity.Property(x => x.VisiblePassword)
                  .HasMaxLength(256);

            entity.Property(x => x.Avatar)
                  .HasMaxLength(500);

            entity.Property(x => x.CCCDImage)
                  .HasMaxLength(500);

            entity.Property(x => x.IsActive)
                  .HasDefaultValue(true);

            entity.Property(x => x.IsSuspended)
                  .HasDefaultValue(false);

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(x => x.UpdatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => x.Email)
                  .IsUnique()
                  .HasFilter("[Email] IS NOT NULL");

            entity.HasIndex(x => x.PhoneNumber)
                  .IsUnique()
                  .HasFilter("[PhoneNumber] IS NOT NULL");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(x => x.Id).HasColumnName("RoleId");
            entity.Property(x => x.Name).HasMaxLength(256);
            entity.Property(x => x.NormalizedName).HasMaxLength(256);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => x.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique();
        });

        // =========================
        // Buildings
        // =========================
        modelBuilder.Entity<Building>()
            .Property(x => x.Latitude)
            .HasColumnType("float");

        modelBuilder.Entity<Building>()
            .Property(x => x.Longitude)
            .HasColumnType("float");

        modelBuilder.Entity<Building>()
            .Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Building>()
            .HasOne(x => x.User)
            .WithMany(x => x.Buildings)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // =========================
        // Rooms
        // =========================
        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("Rooms");
            entity.HasKey(x => x.RoomId);

            entity.Property(x => x.RoomName)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(x => x.Status)
                  .HasDefaultValue("Available")
                  .HasMaxLength(20);

            entity.Property(x => x.Price).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(x => x.ElectricPrice).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(x => x.WaterPrice).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(x => x.UpdatedAt)
      .HasDefaultValueSql("GETDATE()");
            // InternetPrice đã bị xóa

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => new { x.BuildingId, x.RoomName }).IsUnique(); 

            // Relationship với Building
            entity.HasOne(x => x.Building)
                  .WithMany(x => x.Rooms)
                  .HasForeignKey(x => x.BuildingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // =========================
        // RoomImages
        // =========================
        modelBuilder.Entity<RoomImage>()
            .HasOne(x => x.Room)
            .WithMany(x => x.RoomImages)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // Tenants
        // =========================
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(x => x.TenantId);

            entity.Property(x => x.FullName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(x => x.PhoneNumber).HasMaxLength(20);
            entity.Property(x => x.Email).HasMaxLength(100);
            entity.Property(x => x.CCCD).HasMaxLength(20);
            entity.Property(x => x.Gender).HasMaxLength(20);
            entity.Property(x => x.Occupation).HasMaxLength(100);
            entity.Property(x => x.Workplace).HasMaxLength(200);

            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.PhoneNumber).IsUnique();
        });

        // =========================
        // Contracts
        // =========================
        modelBuilder.Entity<Contract>()
            .Property(x => x.Deposit)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        modelBuilder.Entity<Contract>()
            .Property(x => x.RentPrice)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        modelBuilder.Entity<Contract>()
            .Property(x => x.DepositRefundAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        modelBuilder.Entity<Contract>()
            .Property(x => x.DepositDeductionAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        modelBuilder.Entity<Contract>()
            .Property(x => x.PaymentCycle)
            .HasDefaultValue("Monthly");

        modelBuilder.Entity<Contract>()
            .Property(x => x.DepositStatus)
            .HasDefaultValue("Holding");

        modelBuilder.Entity<Contract>()
            .Property(x => x.Status)
            .HasDefaultValue("Active");

        modelBuilder.Entity<Contract>()
            .Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Contract>()
            .HasIndex(x => x.Status);

        modelBuilder.Entity<Contract>()
            .HasIndex(x => x.EndDate);

        modelBuilder.Entity<Contract>()
            .HasOne(x => x.Room)
            .WithMany(x => x.Contracts)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Contract>()
            .HasOne(x => x.Tenant)
            .WithMany(x => x.Contracts)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Contract>()
            .HasOne(x => x.ParentContract)
            .WithMany()
            .HasForeignKey(x => x.ParentContractId)
            .OnDelete(DeleteBehavior.Restrict);

        // =========================
        // Services
        // =========================
        modelBuilder.Entity<Service>()
            .Property(x => x.UnitPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Service>()
            .Property(x => x.ServiceName)
            .HasMaxLength(100);

        modelBuilder.Entity<Service>()
            .Property(x => x.BillingCycle)
            .HasMaxLength(20)
            .HasDefaultValue("Monthly");

        modelBuilder.Entity<Service>()
            .Property(x => x.Icon)
            .HasMaxLength(50);

        modelBuilder.Entity<Service>()
            .HasIndex(x => x.ServiceName)
            .IsUnique();

        // =========================
        // RoomServices
        // =========================
        modelBuilder.Entity<RoomService>()
            .HasOne(x => x.Room)
            .WithMany(x => x.RoomServices)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RoomService>()
            .HasOne(x => x.Service)
            .WithMany(x => x.RoomServices)
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // DeviceCatalogs (danh mục thiết bị dùng chung - seed sẵn)
        // =========================
        modelBuilder.Entity<DeviceCatalog>(entity =>
        {
            entity.HasKey(x => x.DeviceCatalogId);

            entity.Property(x => x.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(x => x.Icon)
                  .HasMaxLength(50);

            entity.HasIndex(x => x.Name).IsUnique();
        });

        // =========================
        // Devices (bảng nối phòng <-> danh mục thiết bị)
        // =========================
        modelBuilder.Entity<Device>()
            .Property(x => x.Quantity)
            .HasDefaultValue(1);

        modelBuilder.Entity<Device>()
            .Property(x => x.Status)
            .HasDefaultValue("Working");

        modelBuilder.Entity<Device>()
            .HasOne(x => x.Room)
            .WithMany(x => x.Devices)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Device>()
            .HasOne(x => x.DeviceCatalog)
            .WithMany(x => x.Devices)
            .HasForeignKey(x => x.DeviceCatalogId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // =========================
        // Vehicles
        // =========================
        modelBuilder.Entity<Vehicle>()
            .Property(x => x.LicensePlateNumber)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<Vehicle>()
            .HasIndex(x => x.LicensePlateNumber)
            .IsUnique();

        modelBuilder.Entity<Vehicle>()
            .Property(x => x.Status)
            .HasMaxLength(20)
            .HasDefaultValue("active");

        modelBuilder.Entity<Vehicle>()
            .Property(x => x.ParkingFee)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        modelBuilder.Entity<Vehicle>()
            .Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Vehicle>()
            .Property(x => x.UpdatedAt)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Vehicle>()
            .HasOne(x => x.Tenant)
            .WithMany(x => x.Vehicles)
            .HasForeignKey(x => x.TenantId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Vehicle>()
            .HasOne(x => x.Room)
            .WithMany(x => x.Vehicles)
            .HasForeignKey(x => x.RoomId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // =========================
        // UtilityUsages
        // =========================
        modelBuilder.Entity<UtilityUsage>()
            .HasKey(x => x.UsageId);

        modelBuilder.Entity<UtilityUsage>()
            .Property(x => x.ElectricNumberBf)
            .HasDefaultValue(0);

        modelBuilder.Entity<UtilityUsage>()
            .Property(x => x.ElectricNumberAt)
            .HasDefaultValue(0);

        modelBuilder.Entity<UtilityUsage>()
            .Property(x => x.ElectricConsumed)
            .HasComputedColumnSql("[ElectricNumberAt] - [ElectricNumberBf]");

        modelBuilder.Entity<UtilityUsage>()
            .Property(x => x.WaterNumberBf)
            .HasDefaultValue(0);

        modelBuilder.Entity<UtilityUsage>()
            .Property(x => x.WaterNumberAt)
            .HasDefaultValue(0);

        modelBuilder.Entity<UtilityUsage>()
            .Property(x => x.WaterConsumed)
            .HasComputedColumnSql("[WaterNumberAt] - [WaterNumberBf]");

        modelBuilder.Entity<UtilityUsage>()
            .Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<UtilityUsage>()
            .HasIndex(x => new { x.RoomId, x.MonthYear });

        modelBuilder.Entity<UtilityUsage>()
            .HasOne(x => x.Room)
            .WithMany(x => x.UtilityUsages)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // Invoices
        // =========================
        modelBuilder.Entity<Invoice>()
            .Property(x => x.RoomFee).HasDefaultValue(0m);
        modelBuilder.Entity<Invoice>()
            .Property(x => x.ElectricFee).HasDefaultValue(0m);
        modelBuilder.Entity<Invoice>()
            .Property(x => x.WaterFee).HasDefaultValue(0m);
        modelBuilder.Entity<Invoice>()
            .Property(x => x.ServiceFee).HasDefaultValue(0m);
        modelBuilder.Entity<Invoice>()
            .Property(x => x.ParkingFee).HasDefaultValue(0m);
        modelBuilder.Entity<Invoice>()
            .Property(x => x.OtherFee).HasDefaultValue(0m);
        modelBuilder.Entity<Invoice>()
            .Property(x => x.DiscountAmount).HasDefaultValue(0m);

        modelBuilder.Entity<Invoice>()
            .Property(x => x.Status)
            .HasDefaultValue("Unpaid");

        modelBuilder.Entity<Invoice>()
            .Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Invoice>()
            .HasIndex(x => x.Status);
        modelBuilder.Entity<Invoice>()
            .HasIndex(x => new { x.RoomId, x.MonthYear });

        modelBuilder.Entity<Invoice>()
            .HasOne(x => x.Room)
            .WithMany(x => x.Invoices)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invoice>()
            .HasOne(x => x.User)
            .WithMany(x => x.Invoices)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // =========================
        // InvoiceDetails
        // =========================
        modelBuilder.Entity<InvoiceDetail>()
            .Property(x => x.Quantity)
            .HasDefaultValue(1);

        modelBuilder.Entity<InvoiceDetail>()
            .Property(x => x.UnitPrice)
            .HasDefaultValue(0m);

        modelBuilder.Entity<InvoiceDetail>()
            .Property(x => x.Amount)
            .HasDefaultValue(0m);

        modelBuilder.Entity<InvoiceDetail>()
            .HasOne(x => x.Invoice)
            .WithMany(x => x.InvoiceDetails)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // Payments
        // =========================
        modelBuilder.Entity<Payment>()
            .Property(x => x.PaymentDate)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Payment>()
            .Property(x => x.Status)
            .HasDefaultValue("Success");

        modelBuilder.Entity<Payment>()
            .HasOne(x => x.Invoice)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // Notifications
        // =========================
        modelBuilder.Entity<Notification>()
            .Property(x => x.IsRead)
            .HasDefaultValue(false);

        modelBuilder.Entity<Notification>()
            .Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Notification>()
            .HasOne(x => x.User)
            .WithMany(x => x.Notifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // Expenses
        // =========================
        modelBuilder.Entity<Expense>()
            .HasOne(x => x.Building)
            .WithMany(x => x.Expenses)
            .HasForeignKey(x => x.BuildingId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // Posts
        // =========================
        modelBuilder.Entity<Post>()
            .Property(x => x.Status)
            .HasDefaultValue("Active");

        modelBuilder.Entity<Post>()
            .Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Post>()
            .HasOne(x => x.Room)
            .WithMany(x => x.Posts)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // Packages (SaaS plans)
        // =========================
        modelBuilder.Entity<Package>(entity =>
        {
            entity.ToTable("Packages");
            entity.HasKey(x => x.PackageId);
            entity.Property(x => x.PackageName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.Price).HasColumnType("decimal(18,2)");
            entity.Property(x => x.IsEnabled).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("GETDATE()");
            entity.HasIndex(x => x.PackageName).IsUnique();
        });

        // =========================
        // Subscriptions
        // =========================
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("Subscriptions");
            entity.HasKey(x => x.SubscriptionId);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Active");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("GETDATE()");
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.EndDate);
            entity.HasOne(x => x.Owner)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Package)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.PackageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // Subscription Payments (SaaS billing)
        // =========================
        modelBuilder.Entity<SubscriptionPayment>(entity =>
        {
            entity.ToTable("SubscriptionPayments");
            entity.HasKey(x => x.PaymentId);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.PaymentMethod).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Success");
            entity.Property(x => x.PaymentDate).HasDefaultValueSql("GETDATE()");
            entity.HasIndex(x => x.PaymentDate);
            entity.HasOne(x => x.Owner)
                .WithMany(x => x.SubscriptionPayments)
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Subscription)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // Audit Logs
        // =========================
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.LogId);
            entity.Property(x => x.Action).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Entity).HasMaxLength(100);
            entity.Property(x => x.IPAddress).HasMaxLength(45);
            entity.Property(x => x.Details).HasMaxLength(500);
            entity.Property(x => x.Timestamp).HasDefaultValueSql("GETDATE()");
            entity.HasIndex(x => x.Timestamp);
            entity.HasIndex(x => x.Action);
            entity.HasOne(x => x.User)
                .WithMany(x => x.AuditLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var decimalProperties = entityType
                .GetProperties()
                .Where(property => property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?));

            foreach (var property in decimalProperties)
            {
                if (property.GetColumnType() is null && property.GetPrecision() is null)
                {
                    property.SetPrecision(18);
                    property.SetScale(2);
                }
            }
        }
    }
}
