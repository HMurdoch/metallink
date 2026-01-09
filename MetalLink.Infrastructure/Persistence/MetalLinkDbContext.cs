using Microsoft.EntityFrameworkCore;
using MetalLink.Domain.Entities;

namespace MetalLink.Infrastructure.Persistence;

public class MetalLinkDbContext : DbContext
{
      public MetalLinkDbContext(DbContextOptions<MetalLinkDbContext> options)
            : base(options)
      {
      }

      // Existing sets
      public DbSet<Customer> Customers => Set<Customer>();
      public DbSet<Operator> Operators => Set<Operator>();
      public DbSet<CustomerDocument> CustomerDocuments => Set<CustomerDocument>();
      public DbSet<Ticket> Tickets => Set<Ticket>();

      // NEW sets
      public DbSet<Company> Companies => Set<Company>();
      public DbSet<Site> Sites => Set<Site>();
      public DbSet<Province> Provinces => Set<Province>();
      public DbSet<Country> Countries => Set<Country>();

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
            base.OnModelCreating(modelBuilder);

            ConfigureCustomer(modelBuilder);
            ConfigureOperator(modelBuilder);
            ConfigureCustomerDocument(modelBuilder);
            ConfigureTicket(modelBuilder);

            // NEW
            ConfigureCompany(modelBuilder);
            ConfigureSite(modelBuilder);
            ConfigureProvince(modelBuilder);
            ConfigureCountry(modelBuilder);
      }

      // -------------------------
      // CUSTOMER
      // -------------------------

      private void ConfigureCustomer(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<Customer>();

            entity.ToTable("customers", "metal_link");

            entity.HasKey(c => c.CustomerId);

            entity.Property(c => c.CustomerId)
                  .HasColumnName("customer_id")
                  .ValueGeneratedOnAdd();

            // Company is required
            entity.Property(c => c.CompanyId)
                  .HasColumnName("company_id")
                  .IsRequired(false); // long + required

            // Site is required (your business rule)
            entity.Property(c => c.SiteId)
                  .HasColumnName("site_id")
                  .IsRequired(false); // ✅ NO IsRequired(false)

            entity.Property(c => c.FirstName)
                  .HasColumnName("first_name")
                  .HasMaxLength(50);

            entity.Property(c => c.LastName)
                  .HasColumnName("last_name")
                  .HasMaxLength(50);

            entity.Property(c => c.IsCompany)
                  .HasColumnName("is_company");

            entity.Property(c => c.IdNumber)
                  .HasColumnName("id_number")
                  .HasMaxLength(20);

            entity.Property(c => c.AccountNumber)
                  .HasColumnName("account_number")
                  .HasColumnType("bigint")
                  .ValueGeneratedOnAdd();

            entity.Property(c => c.PriceCode)
                  .HasColumnName("price_code")
                  .HasMaxLength(10);

            entity.Property(c => c.PhoneNumber)
                  .HasColumnName("phone_number")
                  .HasMaxLength(20);

            entity.Property(c => c.MobileNumber)
                  .HasColumnName("mobile_number")
                  .HasMaxLength(20);

            entity.Property(c => c.Email)
                  .HasColumnName("email")
                  .HasMaxLength(100);

            entity.Property(c => c.Taxable)
                  .HasColumnName("taxable")
                  .IsRequired();

            entity.Property(c => c.IsActive)
                  .HasColumnName("is_active")
                  .IsRequired();

            entity.Property(c => c.CreatedTime)
                  .HasDefaultValueSql("now()")
                  .HasColumnName("created_time")
                  .ValueGeneratedOnAdd()
                  .IsRequired();

            entity.Property(c => c.UpdatedTime)
                  .HasDefaultValueSql("now()")
                  .HasColumnName("updated_time")
                  .ValueGeneratedOnAddOrUpdate()
                  .IsRequired();
            // Relationships

            entity.HasOne(c => c.Company)
                  .WithMany(co => co.Customers)
                  .HasForeignKey(c => c.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Site)
                  .WithMany(s => s.Customers)
                  .HasForeignKey(c => c.SiteId)
                  .OnDelete(DeleteBehavior.Restrict); // ✅ NO IsRequired(false) here either
      }
      // -------------------------
      // COMPANY
      // -------------------------

      private static void ConfigureCompany(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<Company>();

            entity.ToTable("companies", schema: "metal_link");

            entity.HasKey(c => c.CompanyId)
                  .HasName("pk_companies_company_id");

            entity.Property(c => c.CompanyId)
                  .HasColumnName("company_id")
                  .ValueGeneratedOnAdd();

            entity.Property(c => c.CompanyName)
                  .HasColumnName("company_name")
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(c => c.VatNumber)
                  .HasColumnName("vat_number")
                  .HasMaxLength(50);

            entity.Property(c => c.IsActive)
                  .HasColumnName("is_active")
                  .IsRequired();

            entity.Property(c => c.CreatedTime)
                  .HasDefaultValueSql("now()")
                  .HasColumnName("created_time")
                  .ValueGeneratedOnAdd()
                  .IsRequired();

            entity.Property(c => c.UpdatedTime)
                  .HasDefaultValueSql("now()")
                  .HasColumnName("updated_time")
                  .ValueGeneratedOnAddOrUpdate()
                  .IsRequired();

            entity.HasIndex(c => c.CompanyName)
                  .HasDatabaseName("companies_company_name_idx");

            entity.HasIndex(c => c.VatNumber)
                  .HasDatabaseName("companies_vat_number_idx");

            // Navigation: Company → Sites
            entity.HasMany(c => c.Sites)
                  .WithOne(s => s.Company)
                  .HasForeignKey(s => s.CompanyId)
                  .HasConstraintName("fk_sites_company_id_companies");

            // Navigation: Company → Customers
            entity.HasMany(c => c.Customers)
                  .WithOne(cu => cu.Company)
                  .HasForeignKey(cu => cu.CompanyId)
                  .HasConstraintName("fk_customers_company_id_companies");
      }

      public override int SaveChanges()
      {
            ApplyTimestamps();
            return base.SaveChanges();
      }

      public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
      {
            ApplyTimestamps();
            return base.SaveChangesAsync(cancellationToken);
      }

      private void ApplyTimestamps()
      {
            var now = DateTimeOffset.UtcNow;

            foreach (var entry in ChangeTracker.Entries())
            {
                  if (entry.Entity is Company c)
                  {
                        if (entry.State == EntityState.Added)
                        {
                              if (c.CreatedTime == default) c.CreatedTime = now;
                              c.UpdatedTime = now;
                        }
                        else if (entry.State == EntityState.Modified)
                        {
                              c.UpdatedTime = now;
                        }
                  }

                  // If Site also has these columns, repeat the same for Site
                  // if (entry.Entity is Site s) { ... }
            }
      }

      // -------------------------
      // SITE
      // -------------------------

      private static void ConfigureSite(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<Site>();

            entity.ToTable("sites", schema: "metal_link");

            entity.HasKey(s => s.SiteId)
                  .HasName("pk_sites_site_id");

            entity.Property(s => s.SiteId)
                  .HasColumnName("site_id")
                  .ValueGeneratedOnAdd();

            entity.Property(s => s.CompanyId)
                  .HasColumnName("company_id")
                  .IsRequired();

            entity.Property(s => s.SiteName)
                  .HasColumnName("site_name")
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(s => s.SiteCode)
                  .HasColumnName("site_code")
                  .HasMaxLength(50);

            entity.Property(s => s.AddressLine1)
                  .HasColumnName("address_line1")
                  .HasMaxLength(200);

            entity.Property(s => s.AddressLine2)
                  .HasColumnName("address_line2")
                  .HasMaxLength(200);

            entity.Property(s => s.Suburb)
                  .HasColumnName("suburb")
                  .HasMaxLength(100);

            entity.Property(s => s.City)
                  .HasColumnName("city")
                  .HasMaxLength(100);

            entity.Property(s => s.PostalCode)
                  .HasColumnName("postal_code")
                  .HasMaxLength(20);

            entity.Property(s => s.ProvinceId)
                  .HasColumnName("province_id")
                  .IsRequired(false);

            entity.Property(s => s.CountryId)
                  .HasColumnName("country_id")
                  .IsRequired(false);

            entity.Property(s => s.IsActive)
                  .HasColumnName("is_active")
                  .IsRequired();

            entity.Property(c => c.CreatedTime)
                  .HasDefaultValueSql("now()")
                  .HasColumnName("created_time")
                  .ValueGeneratedOnAdd()
                  .IsRequired();

            entity.Property(c => c.UpdatedTime)
                  .HasDefaultValueSql("now()")
                  .HasColumnName("updated_time")
                  .ValueGeneratedOnAddOrUpdate()
                  .IsRequired();

            entity.HasIndex(s => s.SiteCode)
                  .HasDatabaseName("sites_site_code_idx");

            entity.HasIndex(s => new { s.CompanyId, s.SiteName })
                  .HasDatabaseName("sites_company_id_site_name_idx");

            // FK → Company
            entity.HasOne(s => s.Company)
                  .WithMany(c => c.Sites)
                  .HasForeignKey(s => s.CompanyId)
                  .HasConstraintName("fk_sites_company_id_companies");

            // FK → Province (optional/required depending on your model)
            entity.HasOne(s => s.Province)
                  .WithMany(p => p.Sites)
                  .HasForeignKey(s => s.ProvinceId)
                  .HasConstraintName("fk_sites_province_id_provinces");

            // NEW: FK → Country
            entity.HasOne(s => s.Country)
                  .WithMany(c => c.Sites)
                  .HasForeignKey(s => s.CountryId)
                  .HasConstraintName("fk_sites_country_id_countries");
      }

      // -------------------------
      // PROVINCE
      // -------------------------

      private static void ConfigureProvince(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<Province>();

            entity.ToTable("provinces", schema: "metal_link");

            entity.HasKey(p => p.ProvinceId)
                  .HasName("pk_provinces_province_id");

            entity.Property(p => p.ProvinceId)
                  .HasColumnName("province_id")
                  .ValueGeneratedOnAdd();

            entity.Property(p => p.ProvinceName)
                  .HasColumnName("name")
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(p => p.ProvinceCode)
                  .HasColumnName("code")
                  .IsRequired()
                  .HasMaxLength(10);

            entity.Property(p => p.IsActive)
                  .HasColumnName("is_active")
                  .IsRequired();

            entity.Property(p => p.CreatedTime)
                  .HasColumnName("created_time")
                  .IsRequired();

            entity.Property(p => p.UpdatedTime)
                  .HasColumnName("updated_time")
                  .IsRequired();

            entity.HasIndex(p => p.ProvinceCode)
                  .HasDatabaseName("provinces_code_idx");

            entity.HasIndex(p => p.ProvinceName)
                  .HasDatabaseName("provinces_name_idx");

            // Navigation: Province → Sites
            entity.HasMany(p => p.Sites)
                  .WithOne(s => s.Province)
                  .HasForeignKey(s => s.ProvinceId)
                  .HasConstraintName("fk_sites_province_id_provinces");
      }

      // -------------------------
      // COUNTRY
      // -------------------------

      private static void ConfigureCountry(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<Country>();

            entity.ToTable("countries", schema: "metal_link");

            entity.HasKey(c => c.CountryId)
                  .HasName("pk_countries_country_id");

            entity.Property(c => c.CountryId)
                  .HasColumnName("country_id")
                  .ValueGeneratedOnAdd();

            entity.Property(c => c.Name)
                  .HasColumnName("name")
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(c => c.Code)
                  .HasColumnName("code")
                  .HasMaxLength(10);

            entity.Property(c => c.IsActive)
                  .HasColumnName("is_active")
                  .IsRequired();

            entity.Property(c => c.CreatedTime)
                  .HasDefaultValueSql("now()")
                  .HasColumnName("created_time")
                  .ValueGeneratedOnAdd()
                  .IsRequired();

            entity.Property(c => c.UpdatedTime)
                  .HasDefaultValueSql("now()")
                  .HasColumnName("updated_time")
                  .ValueGeneratedOnAddOrUpdate()
                  .IsRequired();

            entity.HasIndex(c => c.Code)
                  .HasDatabaseName("countries_code_idx");

            entity.HasIndex(c => c.Name)
                  .HasDatabaseName("countries_name_idx");
      }

      // -------------------------
      // OPERATOR
      // -------------------------

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

      // -------------------------
      // CUSTOMER DOCUMENT
      // -------------------------

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

      // -------------------------
      // TICKET
      // -------------------------

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

            // Optional: FK to Site for referential integrity
            entity.HasOne<Site>()
                  .WithMany()
                  .HasForeignKey(t => t.SiteId)
                  .HasConstraintName("fk_tickets_site_id_sites");
      }
}
