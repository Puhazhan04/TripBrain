using Microsoft.EntityFrameworkCore;
using TripBrain.Models;
using System.Text.Json;

namespace TripBrain.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<DayPlan> DayPlans => Set<DayPlan>();
    public DbSet<DayActivity> DayActivities => Set<DayActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Trip.Interests value converter
        modelBuilder.Entity<Trip>()
            .Property(t => t.Interests)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            );

        // Configure cascade deletes
        modelBuilder.Entity<Trip>()
            .HasMany(t => t.DayPlans)
            .WithOne(dp => dp.Trip)
            .HasForeignKey(dp => dp.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DayPlan>()
            .HasMany(dp => dp.Activities)
            .WithOne(a => a.DayPlan)
            .HasForeignKey(a => a.DayPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
