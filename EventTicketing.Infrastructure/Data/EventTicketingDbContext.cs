using EventTicketing.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Infrastructure.Data;

public class EventTicketingDbContext : DbContext
{
    public EventTicketingDbContext(DbContextOptions<EventTicketingDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<PricingTier> PricingTiers => Set<PricingTier>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Venue).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.HasIndex(e => e.Date);
        });

        modelBuilder.Entity<PricingTier>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.HasOne(p => p.Event)
                  .WithMany(e => e.PricingTiers)
                  .HasForeignKey(p => p.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.TicketNumber).IsRequired().HasMaxLength(50);
            entity.Property(t => t.PurchaserEmail).IsRequired().HasMaxLength(255);
            entity.Property(t => t.PurchaserName).IsRequired().HasMaxLength(200);
            entity.Property(t => t.PricePaid).HasPrecision(18, 2);
            entity.HasIndex(t => t.TicketNumber).IsUnique();
            entity.HasOne(t => t.Event)
                  .WithMany(e => e.Tickets)
                  .HasForeignKey(t => t.EventId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.PricingTier)
                  .WithMany(p => p.Tickets)
                  .HasForeignKey(t => t.PricingTierId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}