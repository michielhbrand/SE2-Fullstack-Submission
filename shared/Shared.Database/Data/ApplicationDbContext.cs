using Shared.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Shared.Database.Data;

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
    public DbSet<Workflow> Workflows { get; set; }
    public DbSet<WorkflowEvent> WorkflowEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Client entity
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasIndex(e => new { e.Email, e.OrganizationId }).IsUnique();
            entity.Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.OrganizationId);
        });

        // Configure Invoice entity
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.Property(e => e.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasMany(e => e.Items);
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Template)
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.TemplateId);
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
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Template)
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.TemplateId);
        });

        // Configure QuoteItem entity
        modelBuilder.Entity<QuoteItem>(entity =>
        {
            entity.Property(e => e.PricePerUnit).HasPrecision(18, 2);
        });

        // Configure Template entity
        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasIndex(e => new { e.Name, e.Version, e.OrganizationId }).IsUnique();
            entity.Property(e => e.Created).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Type).IsRequired().HasMaxLength(20)
                .HasConversion<string>();
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.OrganizationId);
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

        // Configure User entity
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

        // Configure OrganizationMember entity
        modelBuilder.Entity<OrganizationMember>(entity =>
        {
            entity.HasKey(e => new { e.OrganizationId, e.UserId });
            
            entity.HasIndex(e => e.UserId);
            
            entity.HasIndex(e => e.OrganizationId);
            
            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50);
        });

        // Configure BankAccount entity
        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.HasIndex(e => e.OrganizationId);
        });

        // Configure Workflow entity
        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Quote)
                .WithMany()
                .HasForeignKey(e => e.QuoteId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Invoice)
                .WithMany()
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Client)
                .WithMany()
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Events)
                .WithOne(ev => ev.Workflow)
                .HasForeignKey(ev => ev.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.QuoteId);
            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.ClientId);
        });

        // Configure WorkflowEvent entity
        modelBuilder.Entity<WorkflowEvent>(entity =>
        {
            entity.Property(e => e.OccurredAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);

            entity.HasIndex(e => e.WorkflowId);
            entity.HasIndex(e => e.OccurredAt);
        });

        // Configure UserDirectory entity
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
