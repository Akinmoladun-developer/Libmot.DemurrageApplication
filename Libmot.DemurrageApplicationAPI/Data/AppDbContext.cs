using Libmot.DemurrageApplication.Models;
//using Libmot.DemurrageApplicationAPI.Models;
using LibmotExpress.DemurrageApi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;



//namespace LibmotExpress.DemurrageApi.Data;

namespace Libmot.DemurrageApplication.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<DemurrageRate> DemurrageRates => Set<DemurrageRate>();
    public DbSet<DemurrageAccrual> DemurrageAccruals => Set<DemurrageAccrual>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ShipmentDocument> ShipmentDocuments => Set<ShipmentDocument>();
    public DbSet<DisputeLog> DisputeLogs => Set<DisputeLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // For Customer
        builder.Entity<Customer>(e =>
        {
            e.HasOne(c => c.User)
             .WithOne(u => u.CustomerProfile)
             .HasForeignKey<Customer>(c => c.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(c => c.CompanyName).HasMaxLength(200);
            e.Property(c => c.State).HasMaxLength(100);
        });

        // Shipment
        builder.Entity<Shipment>(e =>
        {
            e.HasIndex(s => s.WaybillNumber).IsUnique();

            e.Property(s => s.DeclaredValue)
             .HasColumnType("decimal(18,2)");

            e.Property(s => s.Weight)
             .HasColumnType("decimal(18,3)");

            e.HasOne(s => s.Customer)
             .WithMany(c => c.Shipments)
             .HasForeignKey(s => s.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.Driver)
             .WithMany()
             .HasForeignKey(s => s.DriverUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // DemurrageRate
        builder.Entity<DemurrageRate>(e =>
        {
            e.Property(r => r.DailyRate).HasColumnType("decimal(18,2)");
            e.Property(r => r.TierName).HasMaxLength(100);
        });

        // DemurrageAccrual
        builder.Entity<DemurrageAccrual>(e =>
        {
            e.Property(a => a.DailyRateApplied).HasColumnType("decimal(18,2)");
            e.Property(a => a.AccumulatedTotal).HasColumnType("decimal(18,2)");

            e.HasOne(a => a.Shipment)
             .WithMany(s => s.DemurrageAccruals)
             .HasForeignKey(a => a.ShipmentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.DemurrageRate)
             .WithMany(r => r.Accruals)
             .HasForeignKey(a => a.DemurrageRateId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Invoice
        builder.Entity<Invoice>(e =>
        {
            e.HasIndex(i => i.InvoiceNumber).IsUnique();
            e.Property(i => i.TotalAmount).HasColumnType("decimal(18,2)");

            e.HasOne(i => i.Customer)
             .WithMany(c => c.Invoices)
             .HasForeignKey(i => i.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.Shipment)
             .WithMany(s => s.Invoices)
             .HasForeignKey(i => i.ShipmentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Payment
        builder.Entity<Payment>(e =>
        {
            e.Property(p => p.AmountPaid).HasColumnType("decimal(18,2)");

            e.HasOne(p => p.Invoice)
             .WithMany(i => i.Payments)
             .HasForeignKey(p => p.InvoiceId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Notification
        builder.Entity<Notification>(e =>
        {
            e.HasOne(n => n.Shipment)
             .WithMany(s => s.Notifications)
             .HasForeignKey(n => n.ShipmentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(n => n.Customer)
             .WithMany(c => c.Notifications)
             .HasForeignKey(n => n.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ShipmentDocument
        builder.Entity<ShipmentDocument>(e =>
        {
            e.HasOne(d => d.Shipment)
             .WithMany(s => s.Documents)
             .HasForeignKey(d => d.ShipmentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // DisputeLog
        builder.Entity<DisputeLog>(e =>
        {
            e.HasOne(d => d.Shipment)
             .WithMany(s => s.DisputeLogs)
             .HasForeignKey(d => d.ShipmentId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(d => d.Invoice)
             .WithMany()
             .HasForeignKey(d => d.InvoiceId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // DemurrageRate
        builder.Entity<DemurrageRate>().HasData(
            new DemurrageRate
            {
                Id = 1,
                TierName = "Tier 1",
                DayFrom = 8,
                DayTo = 14,
                DailyRate = 2000,
                IsActive = true,
                EffectiveFrom = new DateTime(2025, 1, 1),
                CreatedByUserId = "system",
                CreatedAt = new DateTime(2025, 1, 1)
            },
            new DemurrageRate
            {
                Id = 2,
                TierName = "Tier 2",
                DayFrom = 15,
                DayTo = 21,
                DailyRate = 5000,
                IsActive = true,
                EffectiveFrom = new DateTime(2025, 1, 1),
                CreatedByUserId = "system",
                CreatedAt = new DateTime(2025, 1, 1)
            },
            new DemurrageRate
            {
                Id = 3,
                TierName = "Tier 3",
                DayFrom = 22,
                DayTo = 0,
                DailyRate = 10000,
                IsActive = true,
                EffectiveFrom = new DateTime(2025, 1, 1),
                CreatedByUserId = "system",
                CreatedAt = new DateTime(2025, 1, 1)
            }
        );
    }
}
