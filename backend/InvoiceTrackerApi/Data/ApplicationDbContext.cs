using InvoiceTrackerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }
    public DbSet<Quote> Quotes { get; set; }
    public DbSet<QuoteItem> QuoteItems { get; set; }
    public DbSet<Template> Templates { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<BankAccount> BankAccounts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Client entity
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure Invoice entity
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasMany(e => e.Items);
        });

        // Configure InvoiceItem entity
        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.Property(e => e.PricePerUnit).HasPrecision(18, 2);
        });

        // Configure Quote entity
        modelBuilder.Entity<Quote>(entity =>
        {
            entity.Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasMany(e => e.Items);
         });

        // Configure QuoteItem entity
        modelBuilder.Entity<QuoteItem>(entity =>
        {
            entity.Property(e => e.PricePerUnit).HasPrecision(18, 2);
        });

        // Configure Template entity
        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasIndex(e => new { e.Name, e.Version }).IsUnique();
            entity.Property(e => e.Created).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure Organization entity
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasOne(e => e.Address)
                .WithMany()
                .HasForeignKey(e => e.AddressId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.Property(e => e.UserIds)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
            
            entity.Property(e => e.BankAccountIds)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList());
        });

        // Configure BankAccount entity
        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.HasIndex(e => e.OrganizationId);
        });
    }
}
