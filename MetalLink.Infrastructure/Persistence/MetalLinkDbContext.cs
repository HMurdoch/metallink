using Microsoft.EntityFrameworkCore;
using MetalLink.Domain.Entities;

namespace MetalLink.Infrastructure.Persistence;

public class MetalLinkDbContext : DbContext
{
    public MetalLinkDbContext(DbContextOptions<MetalLinkDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCustomer(modelBuilder);
    }

    private static void ConfigureCustomer(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Customer>();

        entity.ToTable("customers", schema: "metal_link");

        entity.HasKey(c => c.CustomerId)
              .HasName("pk_customers_customer_id");

        entity.Property(c => c.CustomerId)
              .HasColumnName("customer_id")
              .ValueGeneratedOnAdd();

        entity.Property(c => c.SiteId)
              .HasColumnName("site_id")
              .IsRequired();

        entity.Property(c => c.FullName)
              .HasColumnName("full_name")
              .IsRequired()
              .HasMaxLength(200);

        entity.Property(c => c.IsCompany)
              .HasColumnName("is_company")
              .IsRequired();

        entity.Property(c => c.CompanyName)
              .HasColumnName("company_name")
              .HasMaxLength(200);

        entity.Property(c => c.IdNumber)
              .HasColumnName("id_number")
              .HasMaxLength(50);

        entity.Property(c => c.AccountNumber)
              .HasColumnName("account_number")
              .HasMaxLength(50);

        entity.Property(c => c.PriceCode)
              .HasColumnName("price_code")
              .HasMaxLength(50);

        entity.Property(c => c.AddressLine1)
              .HasColumnName("address_line1")
              .HasMaxLength(200);

        entity.Property(c => c.AddressLine2)
              .HasColumnName("address_line2")
              .HasMaxLength(200);

        entity.Property(c => c.Suburb)
              .HasColumnName("suburb")
              .HasMaxLength(100);

        entity.Property(c => c.City)
              .HasColumnName("city")
              .HasMaxLength(100);

        entity.Property(c => c.PostalCode)
              .HasColumnName("postal_code")
              .HasMaxLength(20);

        entity.Property(c => c.PhoneNumber)
              .HasColumnName("phone_number")
              .HasMaxLength(50);

        entity.Property(c => c.MobileNumber)
              .HasColumnName("mobile_number")
              .HasMaxLength(50);

        entity.Property(c => c.Email)
              .HasColumnName("email")
              .HasMaxLength(200);

        entity.Property(c => c.IsActive)
              .HasColumnName("is_active")
              .IsRequired();

        entity.Property(c => c.CreatedTime)
              .HasColumnName("created_time")
              .IsRequired();

        entity.Property(c => c.UpdatedTime)
              .HasColumnName("updated_time")
              .IsRequired();

        entity.HasIndex(c => c.AccountNumber)
              .HasDatabaseName("customers_account_number_idx");

        entity.HasIndex(c => c.IdNumber)
              .HasDatabaseName("customers_id_number_idx");
    }
}
