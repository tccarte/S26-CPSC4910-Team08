using Microsoft.EntityFrameworkCore;
using DriverRewards.Models;

namespace DriverRewards.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Admin> Admins { get; set; }
    public DbSet<Behavior> Behaviors { get; set; }
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Sponsor> Sponsors { get; set; }
    public DbSet<SponsorCatalogProduct> SponsorCatalogProducts { get; set; }
    public DbSet<SponsorChangeRequest> SponsorChangeRequests { get; set; }
    public DbSet<DriverNotification> DriverNotifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SponsorCatalogProduct>()
            .HasIndex(scp => new { scp.SponsorId, scp.ProductId })
            .IsUnique();

        modelBuilder.Entity<SponsorCatalogProduct>()
            .HasOne(scp => scp.Sponsor)
            .WithMany()
            .HasForeignKey(scp => scp.SponsorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Driver)
            .WithMany()
            .HasForeignKey(o => o.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DriverNotification>()
            .HasOne(n => n.Driver)
            .WithMany()
            .HasForeignKey(n => n.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
