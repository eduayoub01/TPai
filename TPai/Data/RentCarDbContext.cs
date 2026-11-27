using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TPai.Models;

namespace TPai.Data;

public class RentCarDbContext : IdentityDbContext<ApplicationUser>
{
    public RentCarDbContext(DbContextOptions<RentCarDbContext> options)
        : base(options)
    {
    }

    public DbSet<Car> Cars => Set<Car>();
    public DbSet<CarCategory> CarCategories => Set<CarCategory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Car>()
            .HasOne(c => c.CarCategory)
            .WithMany()
            .HasForeignKey(c => c.CarCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
