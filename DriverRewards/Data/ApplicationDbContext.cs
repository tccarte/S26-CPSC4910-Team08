using Microsoft.EntityFrameworkCore;
using DriverRewards.Models;

namespace DriverRewards.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Driver> Drivers { get; set; }
    public DbSet<Sponsor> Sponsors { get; set; }
    public DbSet<SponsorChangeRequest> SponsorChangeRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure your entity relationships and constraints here
    }
}
