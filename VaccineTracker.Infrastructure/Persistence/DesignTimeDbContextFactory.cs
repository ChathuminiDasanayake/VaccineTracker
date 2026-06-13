using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VaccineTracker.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VaccineTrackerDbContext>
{
    public VaccineTrackerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VaccineTrackerDbContext>();

        var connectionString = "Server=localhost;Database=VaccineDb;User Id=sa;Password=123;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(connectionString);

        return new VaccineTrackerDbContext(optionsBuilder.Options);
    }
}
