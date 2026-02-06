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
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserDirectory> UserDirectory { get; set; }

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
            
            entity.Property(e => e.BankAccountIds)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList());
            
            entity.HasMany(e => e.Members)
                .WithOne(m => m.Organization)
                .HasForeignKey(m => m.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure User entity (minimal table with only business/state fields)
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.Active)
                .IsRequired()
                .HasDefaultValue(true);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            entity.Property(e => e.UpdatedAt);
            
            entity.HasMany(e => e.OrganizationMemberships)
                .WithOne()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure OrganizationMember entity (join table for Keycloak users)
        modelBuilder.Entity<OrganizationMember>(entity =>
        {
            // Composite primary key
            entity.HasKey(e => new { e.OrganizationId, e.UserId });
            
            // Index on UserId for efficient queries by user
            entity.HasIndex(e => e.UserId);
            
            // Index on OrganizationId for efficient queries by organization
            entity.HasIndex(e => e.OrganizationId);
            
            // UserId is a string reference to Keycloak - NO foreign key constraint
            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(255);
            
            // Role field
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50);
        });

        // Configure BankAccount entity
        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.HasIndex(e => e.OrganizationId);
        });

        // Configure UserDirectory entity (read-only model)
        modelBuilder.Entity<UserDirectory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.LastSyncedAt);
            
            entity.Property(e => e.Id)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.FirstName)
                .HasMaxLength(100);
            
            entity.Property(e => e.LastName)
                .HasMaxLength(100);
            
            entity.Property(e => e.Roles)
                .HasMaxLength(500);
            
            entity.Property(e => e.KeycloakEnabled)
                .IsRequired()
                .HasDefaultValue(true);
            
            entity.Property(e => e.Active)
                .IsRequired()
                .HasDefaultValue(true);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            entity.Property(e => e.UpdatedAt);
            
            entity.Property(e => e.LastSyncedAt)
                .IsRequired();
        });
    }
}
