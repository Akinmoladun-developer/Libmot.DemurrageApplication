using Libmot.DemurrageApplicationAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace Libmot.DemurrageApplicationAPI.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) 
            : base(options) 
        {
        }

        public DbSet<Shipment> Shipments => Set<Shipment>();
        public DbSet<Container> Containers => Set<Container>();
        public DbSet<DemurrageTier> DemurrageTiers => Set<DemurrageTier>();
        public DbSet<DemurrageRecord> DemurrageRecords => Set<DemurrageRecord>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Document> Documents => Set<Document>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Shipment → Customer (restrict delete)
            builder.Entity<Shipment>()
                .HasOne(s => s.Customer)
                .WithMany(u => u.Shipments)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Shipment → Driver (no action)
            builder.Entity<Shipment>()
                .HasOne(s => s.AssignedDriver)
                .WithMany()
                .HasForeignKey(s => s.AssignedDriverId)
                .OnDelete(DeleteBehavior.NoAction);

            // Invoice → Customer
            builder.Entity<Invoice>()
                .HasOne(i => i.Customer)
                .WithMany()
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Decimal precision
            builder.Entity<DemurrageTier>()
                .Property(d => d.RatePerDay).HasColumnType("decimal(18,2)");
            builder.Entity<DemurrageRecord>()
                .Property(d => d.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Entity<Invoice>()
                .Property(i => i.SubTotal).HasColumnType("decimal(18,2)");
            builder.Entity<Invoice>()
                .Property(i => i.TaxAmount).HasColumnType("decimal(18,2)");
            builder.Entity<Invoice>()
                .Property(i => i.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Entity<Invoice>()
                .Property(i => i.AmountPaid).HasColumnType("decimal(18,2)");
            builder.Entity<InvoiceLineItem>()
                .Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");

            // Ignore computed property
            builder.Entity<Invoice>()
                .Ignore(i => i.BalanceDue);
            builder.Entity<InvoiceLineItem>()
                .Ignore(i => i.LineTotal);

            // Indexes
            builder.Entity<Shipment>()
                .HasIndex(s => s.TrackingNumber).IsUnique();
            builder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber).IsUnique();
        }

    }
}
