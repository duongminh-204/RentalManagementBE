using Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Backend;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RentalManagementDb>
{
    public RentalManagementDb CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RentalManagementDb>();
        optionsBuilder.UseSqlServer(
            "Server=localhost\\SQLEXPRESS;Database=RentalManagement;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");

        return new RentalManagementDb(optionsBuilder.Options);
    }
}
