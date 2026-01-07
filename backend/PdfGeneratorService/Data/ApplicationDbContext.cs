using Microsoft.EntityFrameworkCore;
using PdfGeneratorService.Models;

namespace PdfGeneratorService.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Invoice entity
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClientName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ClientSurname).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ClientAddress).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ClientCellphone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ModifiedBy).HasMaxLength(255);
            entity.Property(e => e.PdfStorageKey).HasMaxLength(500);
        });

        // Configure InvoiceItem entity
        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            
            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.Items)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
