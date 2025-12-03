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
    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<CustomerDocument> CustomerDocuments => Set<CustomerDocument>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCustomer(modelBuilder);
        ConfigureOperator(modelBuilder);
        ConfigureCustomerDocument(modelBuilder);
        ConfigureTicket(modelBuilder);
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

    private static void ConfigureOperator(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Operator>();

        entity.ToTable("operators", schema: "metal_link");

        entity.HasKey(o => o.OperatorId)
              .HasName("pk_operators_operator_id");

        entity.Property(o => o.OperatorId)
              .HasColumnName("operator_id")
              .ValueGeneratedOnAdd();

        entity.Property(o => o.SiteId)
              .HasColumnName("site_id")
              .IsRequired();

        entity.Property(o => o.Username)
              .HasColumnName("username")
              .IsRequired()
              .HasMaxLength(100);

        entity.Property(o => o.DisplayName)
              .HasColumnName("display_name")
              .IsRequired()
              .HasMaxLength(200);

        entity.Property(o => o.PasswordHash)
              .HasColumnName("password_hash")
              .IsRequired()
              .HasMaxLength(512);

        entity.Property(o => o.Role)
              .HasColumnName("role")
              .IsRequired()
              .HasMaxLength(50);

        entity.Property(o => o.IsActive)
              .HasColumnName("is_active")
              .IsRequired();

        entity.Property(o => o.CreatedTime)
              .HasColumnName("created_time")
              .IsRequired();

        entity.Property(o => o.UpdatedTime)
              .HasColumnName("updated_time")
              .IsRequired();

        entity.HasIndex(o => o.Username)
              .IsUnique()
              .HasDatabaseName("operators_username_idx");
    }

     private static void ConfigureCustomerDocument(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CustomerDocument>();

        entity.ToTable("customer_documents", schema: "metal_link");

        entity.HasKey(d => d.CustomerDocumentId)
              .HasName("pk_customer_documents_customer_document_id");

        entity.Property(d => d.CustomerDocumentId)
              .HasColumnName("customer_document_id")
              .ValueGeneratedOnAdd();

        entity.Property(d => d.CustomerId)
              .HasColumnName("customer_id")
              .IsRequired();

        entity.Property(d => d.DocumentType)
              .HasColumnName("document_type")
              .IsRequired()
              .HasMaxLength(100);

        entity.Property(d => d.FileName)
              .HasColumnName("file_name")
              .IsRequired()
              .HasMaxLength(255);

        entity.Property(d => d.ContentType)
              .HasColumnName("content_type")
              .IsRequired()
              .HasMaxLength(100);

        entity.Property(d => d.StorageKey)
              .HasColumnName("storage_key")
              .IsRequired()
              .HasMaxLength(500);

        entity.Property(d => d.CreatedTime)
              .HasColumnName("created_time")
                          .IsRequired();

        entity.HasIndex(d => d.CustomerId)
              .HasDatabaseName("customer_documents_customer_id_idx");

        entity.HasOne<Customer>()
              .WithMany()
              .HasForeignKey(d => d.CustomerId)
              .HasConstraintName("fk_customer_documents_customer_id_customers");
    }

private static void ConfigureTicket(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Ticket>();

        entity.ToTable("tickets", schema: "metal_link");

        entity.HasKey(t => t.TicketId)
              .HasName("pk_tickets_ticket_id");

        entity.Property(t => t.TicketId)
              .HasColumnName("ticket_id")
              .ValueGeneratedOnAdd();

        entity.Property(t => t.SiteId)
              .HasColumnName("site_id")
              .IsRequired();

        entity.Property(t => t.CustomerId)
              .HasColumnName("customer_id")
              .IsRequired();

        entity.Property(t => t.OperatorId)
              .HasColumnName("operator_id")
              .IsRequired();

        entity.Property(t => t.TicketNumber)
              .HasColumnName("ticket_number")
              .IsRequired()
              .HasMaxLength(50);

        entity.Property(t => t.TicketType)
              .HasColumnName("ticket_type")
              .IsRequired()
              .HasMaxLength(50);

        entity.Property(t => t.FirstWeightKg)
              .HasColumnName("first_weight_kg")
              .HasColumnType("numeric(18,3)");

        entity.Property(t => t.SecondWeightKg)
              .HasColumnName("second_weight_kg")
              .HasColumnType("numeric(18,3)");

        entity.Property(t => t.NetWeightKg)
              .HasColumnName("net_weight_kg")
              .HasColumnType("numeric(18,3)")
              .IsRequired();

        entity.Property(t => t.UnitPricePerKg)
              .HasColumnName("unit_price_per_kg")
              .HasColumnType("numeric(18,4)")
              .IsRequired();

        entity.Property(t => t.TotalAmount)
              .HasColumnName("total_amount")
              .HasColumnType("numeric(18,2)")
              .IsRequired();

        entity.Property(t => t.CurrencyCode)
              .HasColumnName("currency_code")
              .IsRequired()
              .HasMaxLength(10);

        entity.Property(t => t.ProductDescription)
              .HasColumnName("product_description")
              .HasMaxLength(200);

        entity.Property(t => t.Notes)
              .HasColumnName("notes")
              .HasMaxLength(500);

        entity.Property(t => t.CreatedTime)
              .HasColumnName("created_time")
              .IsRequired();

        entity.Property(t => t.UpdatedTime)
              .HasColumnName("updated_time")
              .IsRequired();

        entity.HasIndex(t => t.TicketNumber)
              .IsUnique()
              .HasDatabaseName("tickets_ticket_number_idx");

        entity.HasIndex(t => t.CustomerId)
              .HasDatabaseName("tickets_customer_id_idx");

        entity.HasIndex(t => t.SiteId)
              .HasDatabaseName("tickets_site_id_idx");

        entity.HasOne<Customer>()
              .WithMany()
              .HasForeignKey(t => t.CustomerId)
              .HasConstraintName("fk_tickets_customer_id_customers");

        entity.HasOne<Operator>()
              .WithMany()
              .HasForeignKey(t => t.OperatorId)
              .HasConstraintName("fk_tickets_operator_id_operators");
    }    
}
