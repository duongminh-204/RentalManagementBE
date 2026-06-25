using Backend.Authorization;
using Backend.Data;
using Backend.DTOs.Admin;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly RentalManagementDb _context;
    private int? _ownerRoleId;

    public AdminRepository(RentalManagementDb context)
    {
        _context = context;
    }

    public async Task<int> GetOwnerRoleIdAsync()
    {
        if (_ownerRoleId.HasValue) return _ownerRoleId.Value;
        _ownerRoleId = await _context.Roles
            .Where(r => r.Name == RoleNames.Owner)
            .Select(r => r.Id)
            .FirstAsync();
        return _ownerRoleId.Value;
    }

    private IQueryable<int> GetUserIdsInRole(int roleId) =>
        _context.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId);

    public Task<int> CountTenantsAsync() => _context.Tenants.CountAsync();

    public Task<int> CountRoomsAsync() => _context.Rooms.CountAsync();

    public async Task<AdminDashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var ownerRoleId = await GetOwnerRoleIdAsync();
        var ownerUserIds = GetUserIdsInRole(ownerRoleId);
        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var yearStart = new DateTime(now.Year, 1, 1);

        var owners = await _context.Users
            .Where(u => ownerUserIds.Contains(u.Id))
            .Select(u => new
            {
                u.IsSuspended,
                ActiveSub = u.Subscriptions
                    .Where(s => s.Status == "Active" && s.EndDate >= now)
                    .OrderByDescending(s => s.EndDate)
                    .Select(s => new { s.EndDate, s.Package.Price })
                    .FirstOrDefault(),
                ExpiredSub = u.Subscriptions.Any(s => s.Status == "Expired" || s.EndDate < now)
            })
            .ToListAsync();

        var totalOwners = owners.Count;
        var suspendedOwners = owners.Count(o => o.IsSuspended);
        var activeOwners = owners.Count(o => !o.IsSuspended && o.ActiveSub != null);
        var expiredOwners = owners.Count(o => !o.IsSuspended && o.ActiveSub == null && o.ExpiredSub);

        var monthlyRevenue = await _context.SubscriptionPayments
            .Where(p => p.Status == "Success" && p.PaymentDate >= monthStart)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var annualRevenue = await _context.SubscriptionPayments
            .Where(p => p.Status == "Success" && p.PaymentDate >= yearStart)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var mrr = await _context.Subscriptions
            .Where(s => s.Status == "Active" && s.EndDate >= now)
            .Include(s => s.Package)
            .SumAsync(s => (decimal?)s.Package.Price) ?? 0m;

        return new AdminDashboardSummaryDto
        {
            TotalOwners = totalOwners,
            ActiveOwners = activeOwners,
            ExpiredOwners = expiredOwners,
            SuspendedOwners = suspendedOwners,
            TotalTenants = await CountTenantsAsync(),
            TotalRooms = await CountRoomsAsync(),
            MonthlyRevenue = monthlyRevenue,
            AnnualRevenue = annualRevenue,
            Mrr = mrr,
            Arr = mrr * 12
        };
    }

    public async Task<AdminDashboardChartsDto> GetDashboardChartsAsync()
    {
        var now = DateTime.Now;
        var sixMonthsAgo = now.AddMonths(-5);
        var monthStart = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

        var payments = await _context.SubscriptionPayments
            .Where(p => p.Status == "Success" && p.PaymentDate >= monthStart)
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.Amount) })
            .ToListAsync();

        var revenueGrowth = new List<ChartDataPointDto>();
        for (var i = 0; i < 6; i++)
        {
            var d = monthStart.AddMonths(i);
            var match = payments.FirstOrDefault(p => p.Year == d.Year && p.Month == d.Month);
            revenueGrowth.Add(new ChartDataPointDto
            {
                Label = d.ToString("MM/yyyy"),
                Value = match?.Total ?? 0m
            });
        }

        var packageDistribution = await _context.Subscriptions
            .Where(s => s.Status == "Active" && s.EndDate >= now)
            .GroupBy(s => s.Package.PackageName)
            .Select(g => new ChartDataPointDto
            {
                Label = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var ownerRoleId = await GetOwnerRoleIdAsync();
        var ownerUserIds = GetUserIdsInRole(ownerRoleId);
        var ownerGrowth = await _context.Users
            .Where(u => ownerUserIds.Contains(u.Id) && u.CreatedAt >= monthStart)
            .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync();

        var ownerGrowthChart = new List<ChartDataPointDto>();
        for (var i = 0; i < 6; i++)
        {
            var d = monthStart.AddMonths(i);
            var match = ownerGrowth.FirstOrDefault(o => o.Year == d.Year && o.Month == d.Month);
            ownerGrowthChart.Add(new ChartDataPointDto
            {
                Label = d.ToString("MM/yyyy"),
                Count = match?.Count ?? 0
            });
        }

        var subscriptionStatus = await _context.Subscriptions
            .GroupBy(s => s.Status)
            .Select(g => new ChartDataPointDto
            {
                Label = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        return new AdminDashboardChartsDto
        {
            RevenueGrowth = revenueGrowth,
            PackageDistribution = packageDistribution,
            OwnerGrowth = ownerGrowthChart,
            SubscriptionStatus = subscriptionStatus
        };
    }

    public async Task<(List<AdminOwnerListDto> Items, int Total)> GetOwnersAsync(string? search, string? status, int page, int pageSize)
    {
        var ownerRoleId = await GetOwnerRoleIdAsync();
        var ownerUserIds = GetUserIdsInRole(ownerRoleId);
        var now = DateTime.Now;

        var query = _context.Users
            .AsNoTracking()
            .Where(u => ownerUserIds.Contains(u.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.FullName.Contains(term) ||
                (u.Email != null && u.Email.Contains(term)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
        }

        var owners = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.Avatar,
                u.IsActive,
                u.IsSuspended,
                u.CreatedAt,
                ActiveSub = u.Subscriptions
                    .Where(s => s.Status == "Active" && s.EndDate >= now)
                    .OrderByDescending(s => s.EndDate)
                    .Select(s => new { s.EndDate, PackageName = s.Package.PackageName })
                    .FirstOrDefault(),
                LatestSub = u.Subscriptions
                    .OrderByDescending(s => s.EndDate)
                    .Select(s => new { s.EndDate, PackageName = s.Package.PackageName })
                    .FirstOrDefault(),
                BuildingCount = u.Buildings.Count(),
                RoomCount = u.Buildings.SelectMany(b => b.Rooms).Count()
            })
            .ToListAsync();

        var mapped = owners.Select(o =>
        {
            var subStatus = o.IsSuspended ? "Suspended" :
                o.ActiveSub != null ? "Active" :
                (o.LatestSub != null && o.LatestSub.EndDate < now) ? "Expired" : "None";

            return new AdminOwnerListDto
            {
                OwnerId = o.Id,
                FullName = o.FullName,
                Email = o.Email,
                Phone = o.PhoneNumber,
                Avatar = o.Avatar,
                Package = o.ActiveSub?.PackageName ?? o.LatestSub?.PackageName,
                SubscriptionStatus = subStatus,
                CreatedDate = o.CreatedAt,
                ExpiredDate = o.ActiveSub?.EndDate ?? o.LatestSub?.EndDate,
                IsActive = o.IsActive,
                IsSuspended = o.IsSuspended,
                RoomCount = o.RoomCount,
                BuildingCount = o.BuildingCount
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim();
            mapped = mapped.Where(o =>
                o.SubscriptionStatus.Equals(normalized, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var total = mapped.Count;
        var items = mapped.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, total);
    }

    public async Task<AdminOwnerDetailDto?> GetOwnerByIdAsync(int ownerId)
    {
        var ownerRoleId = await GetOwnerRoleIdAsync();
        var ownerUserIds = GetUserIdsInRole(ownerRoleId);
        var now = DateTime.Now;

        var owner = await _context.Users
            .AsNoTracking()
            .Include(u => u.Subscriptions).ThenInclude(s => s.Package)
            .Include(u => u.Buildings).ThenInclude(b => b.Rooms)
            .FirstOrDefaultAsync(u => u.Id == ownerId && ownerUserIds.Contains(u.Id));

        if (owner == null) return null;

        var activeSub = owner.Subscriptions
            .Where(s => s.Status == "Active" && s.EndDate >= now)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefault();

        var latestSub = owner.Subscriptions.OrderByDescending(s => s.EndDate).FirstOrDefault();
        var subStatus = owner.IsSuspended ? "Suspended" :
            activeSub != null ? "Active" :
            (latestSub != null && latestSub.EndDate < now) ? "Expired" : "None";

        return new AdminOwnerDetailDto
        {
            OwnerId = owner.Id,
            FullName = owner.FullName,
            Email = owner.Email,
            Phone = owner.PhoneNumber,
            Avatar = owner.Avatar,
            CCCD = owner.CCCD,
            Address = owner.Address,
            Package = activeSub?.Package?.PackageName ?? latestSub?.Package?.PackageName,
            PackageId = activeSub?.PackageId ?? latestSub?.PackageId,
            SubscriptionId = activeSub?.SubscriptionId ?? latestSub?.SubscriptionId,
            SubscriptionStatus = subStatus,
            CreatedDate = owner.CreatedAt,
            UpdatedAt = owner.UpdatedAt,
            ExpiredDate = activeSub?.EndDate ?? latestSub?.EndDate,
            SubscriptionStartDate = activeSub?.StartDate ?? latestSub?.StartDate,
            IsActive = owner.IsActive,
            IsSuspended = owner.IsSuspended,
            RoomCount = owner.Buildings.SelectMany(b => b.Rooms).Count(),
            BuildingCount = owner.Buildings.Count
        };
    }

    public async Task<User?> GetOwnerEntityAsync(int ownerId)
    {
        var ownerRoleId = await GetOwnerRoleIdAsync();
        var ownerUserIds = GetUserIdsInRole(ownerRoleId);
        return await _context.Users
            .Include(u => u.Subscriptions)
            .FirstOrDefaultAsync(u => u.Id == ownerId && ownerUserIds.Contains(u.Id));
    }

    public async Task<int> GetOwnerRoomCountAsync(int ownerUserId)
    {
        return await _context.Rooms
            .CountAsync(r => r.Building.UserId == ownerUserId);
    }

    public async Task CleanupOwnerBeforeDeleteAsync(int ownerUserId)
    {
        var invoiceCount = await _context.Invoices.CountAsync(i => i.UserId == ownerUserId);
        if (invoiceCount > 0)
            throw new InvalidOperationException("Không thể xóa chủ trọ đang có hóa đơn.");

        var subscriptionIds = await _context.Subscriptions
            .Where(s => s.OwnerUserId == ownerUserId)
            .Select(s => s.SubscriptionId)
            .ToListAsync();

        if (subscriptionIds.Count > 0)
        {
            var subscriptionPayments = await _context.SubscriptionPayments
                .Where(p => subscriptionIds.Contains(p.SubscriptionId) || p.OwnerUserId == ownerUserId)
                .ToListAsync();
            _context.SubscriptionPayments.RemoveRange(subscriptionPayments);
        }

        var subscriptions = await _context.Subscriptions
            .Where(s => s.OwnerUserId == ownerUserId)
            .ToListAsync();
        _context.Subscriptions.RemoveRange(subscriptions);

        var buildings = await _context.Buildings
            .Where(b => b.UserId == ownerUserId)
            .ToListAsync();
        _context.Buildings.RemoveRange(buildings);

        var notifications = await _context.Notifications
            .Where(n => n.UserId == ownerUserId)
            .ToListAsync();
        _context.Notifications.RemoveRange(notifications);

        await _context.SaveChangesAsync();
    }

    public async Task<(List<AdminPackageDto> Items, int Total)> GetPackagesAsync(string? search, bool? isEnabled, int page, int pageSize)
    {
        var query = _context.Packages.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.PackageName.Contains(term));
        }

        if (isEnabled.HasValue)
            query = query.Where(p => p.IsEnabled == isEnabled.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Price)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new AdminPackageDto
            {
                PackageId = p.PackageId,
                PackageName = p.PackageName,
                Price = p.Price,
                MaxRooms = p.MaxRooms,
                Description = p.Description,
                RoomRange = p.RoomRange,
                TargetAudience = p.TargetAudience,
                IsRecommended = p.IsRecommended,
                Features = PackageFeatureHelper.SplitFeatureLines(p.FeatureLines),
                IsEnabled = p.IsEnabled,
                SubscriberCount = p.Subscriptions.Count(s => s.Status == "Active" && s.EndDate >= DateTime.Now)
            })
            .ToListAsync();

        return (items, total);
    }

    public Task<Package?> GetPackageByIdAsync(int packageId) =>
        _context.Packages.FirstOrDefaultAsync(p => p.PackageId == packageId);

    public async Task AddPackageAsync(Package package)
    {
        _context.Packages.Add(package);
        await _context.SaveChangesAsync();
    }

    public async Task DeletePackageAsync(Package package)
    {
        _context.Packages.Remove(package);
        await _context.SaveChangesAsync();
    }

    public Task<bool> PackageHasSubscriptionsAsync(int packageId) =>
        _context.Subscriptions.AnyAsync(s => s.PackageId == packageId);

    public async Task<(List<AdminSubscriptionDto> Items, int Total)> GetSubscriptionsAsync(string? status, string? search, int page, int pageSize)
    {
        var query = _context.Subscriptions
            .AsNoTracking()
            .Include(s => s.Owner)
            .Include(s => s.Package)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                s.Owner.FullName.Contains(term) ||
                (s.Owner.Email != null && s.Owner.Email.Contains(term)));
        }

        var total = await query.CountAsync();
        var subs = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<AdminSubscriptionDto>();
        foreach (var s in subs)
        {
            var roomCount = await GetOwnerRoomCountAsync(s.OwnerUserId);
            items.Add(new AdminSubscriptionDto
            {
                SubscriptionId = s.SubscriptionId,
                OwnerId = s.OwnerUserId,
                OwnerName = s.Owner.FullName,
                PackageId = s.PackageId,
                PackageName = s.Package.PackageName,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Status = s.Status,
                OwnerRoomCount = roomCount,
                MaxRooms = s.Package.MaxRooms,
                CreatedAt = s.CreatedAt
            });
        }

        return (items, total);
    }

    public async Task<(List<AdminOwnerSubscriptionsGroupDto> Items, int Total)> GetSubscriptionsGroupedByOwnerAsync(string? status, string? search, int page, int pageSize)
    {
        var filteredQuery = _context.Subscriptions
            .AsNoTracking()
            .Include(s => s.Owner)
            .Include(s => s.Package)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            filteredQuery = filteredQuery.Where(s => s.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filteredQuery = filteredQuery.Where(s =>
                s.Owner.FullName.Contains(term) ||
                (s.Owner.Email != null && s.Owner.Email.Contains(term)));
        }

        var ownerIds = await filteredQuery
            .GroupBy(s => s.OwnerUserId)
            .OrderByDescending(g => g.Max(s => s.CreatedAt))
            .Select(g => g.Key)
            .ToListAsync();

        var total = ownerIds.Count;
        var pagedOwnerIds = ownerIds
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = new List<AdminOwnerSubscriptionsGroupDto>();
        foreach (var ownerId in pagedOwnerIds)
        {
            var subs = await _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.Owner)
                .Include(s => s.Package)
                .Where(s => s.OwnerUserId == ownerId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            if (subs.Count == 0) continue;

            var roomCount = await GetOwnerRoomCountAsync(ownerId);
            items.Add(new AdminOwnerSubscriptionsGroupDto
            {
                OwnerId = ownerId,
                OwnerName = subs[0].Owner.FullName,
                OwnerEmail = subs[0].Owner.Email,
                OwnerRoomCount = roomCount,
                Subscriptions = subs.Select(s => new AdminSubscriptionDto
                {
                    SubscriptionId = s.SubscriptionId,
                    OwnerId = s.OwnerUserId,
                    OwnerName = s.Owner.FullName,
                    PackageId = s.PackageId,
                    PackageName = s.Package.PackageName,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Status = s.Status,
                    OwnerRoomCount = roomCount,
                    MaxRooms = s.Package.MaxRooms,
                    CreatedAt = s.CreatedAt
                }).ToList()
            });
        }

        return (items, total);
    }

    public Task<Subscription?> GetSubscriptionByIdAsync(int subscriptionId) =>
        _context.Subscriptions
            .Include(s => s.Package)
            .Include(s => s.Owner)
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);

    public Task<Subscription?> GetActiveSubscriptionAsync(int ownerUserId) =>
        _context.Subscriptions
            .Include(s => s.Package)
            .Where(s => s.OwnerUserId == ownerUserId && s.Status == "Active" && s.EndDate >= DateTime.Now)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

    public async Task AddSubscriptionAsync(Subscription subscription)
    {
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteSubscriptionAsync(Subscription subscription)
    {
        var payments = await _context.SubscriptionPayments
            .Where(p => p.SubscriptionId == subscription.SubscriptionId)
            .ToListAsync();
        if (payments.Count > 0)
            _context.SubscriptionPayments.RemoveRange(payments);

        _context.Subscriptions.Remove(subscription);
        await _context.SaveChangesAsync();
    }

    public async Task ExpireSubscriptionsAsync()
    {
        var now = DateTime.Now;
        var expired = await _context.Subscriptions
            .Where(s => s.Status == "Active" && s.EndDate < now)
            .ToListAsync();

        foreach (var sub in expired)
            sub.Status = "Expired";

        if (expired.Count > 0)
            await _context.SaveChangesAsync();
    }

    public async Task<(List<AdminPaymentDto> Items, int Total)> GetPaymentsAsync(string? status, int? ownerId, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = _context.SubscriptionPayments
            .AsNoTracking()
            .Include(p => p.Owner)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);

        if (ownerId.HasValue)
            query = query.Where(p => p.OwnerUserId == ownerId.Value);

        if (from.HasValue)
            query = query.Where(p => p.PaymentDate >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.PaymentDate <= to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new AdminPaymentDto
            {
                PaymentId = p.PaymentId,
                OwnerId = p.OwnerUserId,
                OwnerName = p.Owner.FullName,
                SubscriptionId = p.SubscriptionId,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                PaymentDate = p.PaymentDate,
                Status = p.Status
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(DateTime? from, DateTime? to)
    {
        var query = _context.SubscriptionPayments.Where(p => p.Status == "Success");

        if (from.HasValue) query = query.Where(p => p.PaymentDate >= from.Value);
        if (to.HasValue) query = query.Where(p => p.PaymentDate <= to.Value);

        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var totalRevenue = await query.SumAsync(p => (decimal?)p.Amount) ?? 0m;
        var monthlyRevenue = await query.Where(p => p.PaymentDate >= monthStart).SumAsync(p => (decimal?)p.Amount) ?? 0m;
        var totalPayments = await query.CountAsync();

        var byMonthRaw = await query
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Value = g.Sum(x => x.Amount)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        var byMonth = byMonthRaw
            .Select(x => new ChartDataPointDto
            {
                Label = x.Month.ToString("00") + "/" + x.Year,
                Value = x.Value
            })
            .ToList();

        return new RevenueReportDto
        {
            TotalRevenue = totalRevenue,
            MonthlyRevenue = monthlyRevenue,
            TotalPayments = totalPayments,
            ByMonth = byMonth
        };
    }

    public async Task AddPaymentAsync(SubscriptionPayment payment)
    {
        _context.SubscriptionPayments.Add(payment);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<AdminUserDto> Items, int Total)> GetUsersAsync(string? role, string? search, bool? isActive, string? subscriptionStatus, int page, int pageSize)
    {
        var now = DateTime.Now;
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleUserIds =
                from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                where r.Name == role
                select ur.UserId;

            query = query.Where(u => roleUserIds.Contains(u.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.FullName.Contains(term) ||
                (u.Email != null && u.Email.Contains(term)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
        }

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.Address,
                u.Avatar,
                u.IsActive,
                u.IsSuspended,
                u.CreatedAt,
                Role = (
                    from ur in _context.UserRoles
                    join r in _context.Roles on ur.RoleId equals r.Id
                    where ur.UserId == u.Id
                    select r.Name
                ).FirstOrDefault(),
                BuildingCount = u.Buildings.Count(),
                RoomCount = u.Buildings.SelectMany(b => b.Rooms).Count(),
                ActiveSub = u.Subscriptions
                    .Where(s => s.Status == "Active" && s.EndDate >= now)
                    .OrderByDescending(s => s.EndDate)
                    .Select(s => new { s.EndDate, PackageName = s.Package.PackageName })
                    .FirstOrDefault(),
                LatestSub = u.Subscriptions
                    .OrderByDescending(s => s.EndDate)
                    .Select(s => new { s.EndDate, PackageName = s.Package.PackageName })
                    .FirstOrDefault(),
            })
            .ToListAsync();

        var mapped = users.Select(u =>
        {
            var isOwner = string.Equals(u.Role, RoleNames.Owner, StringComparison.OrdinalIgnoreCase);
            var subStatus = !isOwner ? null :
                u.IsSuspended ? "Suspended" :
                u.ActiveSub != null ? "Active" :
                (u.LatestSub != null && u.LatestSub.EndDate < now) ? "Expired" : "None";

            return new AdminUserDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Address = u.Address,
                Avatar = u.Avatar,
                Role = u.Role ?? string.Empty,
                IsActive = u.IsActive,
                IsSuspended = u.IsSuspended,
                CreatedAt = u.CreatedAt,
                Package = isOwner ? (u.ActiveSub?.PackageName ?? u.LatestSub?.PackageName) : null,
                SubscriptionStatus = subStatus,
                ExpiredDate = isOwner ? (u.ActiveSub?.EndDate ?? u.LatestSub?.EndDate) : null,
                RoomCount = isOwner ? u.RoomCount : 0,
                BuildingCount = isOwner ? u.BuildingCount : 0,
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(subscriptionStatus))
        {
            var normalized = subscriptionStatus.Trim();
            mapped = mapped.Where(u =>
                u.SubscriptionStatus != null &&
                u.SubscriptionStatus.Equals(normalized, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var total = mapped.Count;
        var items = mapped.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, total);
    }

    public Task<User?> GetUserByIdAsync(int userId) =>
        _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

    public async Task<(List<AdminAuditLogDto> Items, int Total)> GetAuditLogsAsync(int? userId, string? action, string? entity, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = _context.AuditLogs.AsNoTracking().Include(a => a.User).AsQueryable();

        if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(entity)) query = query.Where(a => a.Entity == entity);
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AdminAuditLogDto
            {
                LogId = a.LogId,
                UserId = a.UserId,
                UserName = a.User != null ? a.User.FullName : null,
                Action = a.Action,
                Entity = a.Entity,
                EntityId = a.EntityId,
                IPAddress = a.IPAddress,
                Timestamp = a.Timestamp,
                Details = a.Details
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task AddAuditLogAsync(AuditLog log)
    {
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
