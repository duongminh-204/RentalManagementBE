using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Backend.Data;

public class RentalManagementDb : DbContext
{
    public RentalManagementDb(DbContextOptions<RentalManagementDb> options) : base(options)
    {
    }

    public DbSet<Role> Roles { get; set; }
    public DbSet<User> Users { get; set; }
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureDecimalPrecision(modelBuilder);

        // =========================
        // Roles
        // =========================
        modelBuilder.Entity<Role>()
            .HasIndex(x => x.Name)
            .IsUnique();

        // =========================
        // Users - ĐÃ SỬA & TỐI ƯU
        // =========================
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(x => x.UserId);

            // Required fields
            entity.Property(x => x.FullName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(x => x.PasswordHash)
                  .IsRequired()
                  .HasMaxLength(255);

            // Optional fields with max length
            entity.Property(x => x.Email)
                  .HasMaxLength(100);

            entity.Property(x => x.PhoneNumber)
                  .HasMaxLength(20);

            entity.Property(x => x.CCCD)
                  .HasMaxLength(20);

            entity.Property(x => x.Address)
                  .HasMaxLength(500);

            entity.Property(x => x.Avatar)
                  .HasMaxLength(500);

            entity.Property(x => x.CCCDImage)
                  .HasMaxLength(500);

            // Default values
            entity.Property(x => x.IsActive)
                  .HasDefaultValue(true);

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(x => x.UpdatedAt)
                  .HasDefaultValueSql("GETDATE()");

            // Unique indexes
            entity.HasIndex(x => x.Email)
                  .IsUnique();

            entity.HasIndex(x => x.PhoneNumber)
                  .IsUnique();

            // Relationship
            entity.HasOne(x => x.Role)
                  .WithMany(x => x.Users)
                  .HasForeignKey(x => x.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // =========================
        // Buildings
        // =========================
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
            .HasDefaultValue(0m);

        modelBuilder.Entity<Contract>()
            .Property(x => x.Status)
            .HasDefaultValue("Active");

        modelBuilder.Entity<Contract>()
            .Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Contract>()
            .HasIndex(x => x.Status);

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
