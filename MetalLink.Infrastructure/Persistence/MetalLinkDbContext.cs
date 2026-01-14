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
      public DbSet<TicketLine> TicketLines => Set<TicketLine>();
      public DbSet<Product> Products => Set<Product>();
      public DbSet<Price> Prices => Set<Price>();
      public DbSet<Currency> Currencies => Set<Currency>();

      // NEW sets
      public DbSet<Company> Companies => Set<Company>();
      public DbSet<Site> Sites => Set<Site>();
      public DbSet<Province> Provinces => Set<Province>();
      public DbSet<Country> Countries => Set<Country>();
      public DbSet<Buyer> Buyers => Set<Buyer>();
      public DbSet<StockMovement> StockMovements => Set<StockMovement>();
      
      // Separated Ticket Systems
      public DbSet<TicketReceiving> TicketsReceiving => Set<TicketReceiving>();
      public DbSet<TicketReceivingLine> TicketReceivingLines => Set<TicketReceivingLine>();
      public DbSet<TicketSending> TicketsSending => Set<TicketSending>();
      public DbSet<TicketSendingLine> TicketSendingLines => Set<TicketSendingLine>();
      
      // Stock Management
      public DbSet<StockMovementReceiving> StockMovementsReceiving => Set<StockMovementReceiving>();
      public DbSet<StockMovementSending> StockMovementsSending => Set<StockMovementSending>();
      public DbSet<StockOnHand> StockOnHand => Set<StockOnHand>();

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
            base.OnModelCreating(modelBuilder);

            ConfigureCustomer(modelBuilder);
            ConfigureOperator(modelBuilder);
            ConfigureCustomerDocument(modelBuilder);
            ConfigureTicket(modelBuilder);
            ConfigureTicketLine(modelBuilder);

            // NEW
            ConfigureCompany(modelBuilder);
            ConfigureSite(modelBuilder);
            ConfigureProvince(modelBuilder);
            ConfigureCountry(modelBuilder);
            ConfigureProduct(modelBuilder);
            ConfigurePrice(modelBuilder);
            ConfigureCurrency(modelBuilder);
            ConfigureBuyer(modelBuilder);
            ConfigureStockMovement(modelBuilder);
            
            // Separated Ticket Systems
            ConfigureTicketReceiving(modelBuilder);
            ConfigureTicketReceivingLine(modelBuilder);
            ConfigureTicketSending(modelBuilder);
            ConfigureTicketSendingLine(modelBuilder);
            
            // Stock Management
            ConfigureStockMovementReceiving(modelBuilder);
            ConfigureStockMovementSending(modelBuilder);
            ConfigureStockOnHand(modelBuilder);
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

            // Image paths
            entity.Property(c => c.IdCardImagePath)
                  .HasColumnName("id_card_image_path")
                  .HasMaxLength(500);

            entity.Property(c => c.DriverLicenseImagePath)
                  .HasColumnName("driver_license_image_path")
                  .HasMaxLength(500);

            entity.Property(c => c.PhotoImagePath)
                  .HasColumnName("photo_image_path")
                  .HasMaxLength(500);

            entity.Property(c => c.SignatureImagePath)
                  .HasColumnName("signature_image_path")
                  .HasMaxLength(500);

            entity.Property(c => c.FingerprintImagePath)
                  .HasColumnName("fingerprint_image_path")
                  .HasMaxLength(500);

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
      // CURRENCY
      // -------------------------

      private static void ConfigureCurrency(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<Currency>();

            entity.ToTable("currencies", schema: "metal_link");

            entity.HasKey(c => c.CurrencyId)
                  .HasName("pk_currencies_currency_id");

            entity.Property(c => c.CurrencyId)
                  .HasColumnName("currency_id")
                  .ValueGeneratedOnAdd();

            entity.Property(c => c.CurrencyCode)
                  .HasColumnName("currency_code")
                  .IsRequired()
                  .HasMaxLength(10);

            entity.Property(c => c.CurrencyDescription)
                  .HasColumnName("currency_description")
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(c => c.IsActive)
                  .HasColumnName("is_active")
                  .IsRequired();

            entity.Property(c => c.CreatedTime)
                  .HasColumnName("created_time")
                  .IsRequired();

            entity.Property(c => c.UpdatedTime)
                  .HasColumnName("updated_time")
                  .IsRequired();

            entity.HasIndex(c => c.CurrencyCode)
                  .HasDatabaseName("currencies_currency_code_idx");
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

            // Don't configure navigation - CustomerDocument doesn't have a Customer navigation property
            // Just let EF use the CustomerId FK as-is
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

            // Configure FK properties with ValueGeneratedNever to avoid shadow properties
            entity.Property(t => t.SiteId)
                  .HasColumnName("site_id")
                  .IsRequired()
                  .ValueGeneratedNever();

            entity.Property(t => t.CustomerId)
                  .HasColumnName("customer_id")
                  .IsRequired(false)  // Made nullable for sending tickets
                  .ValueGeneratedNever();

            entity.Property(t => t.BuyerId)
                  .HasColumnName("buyer_id")
                  .IsRequired(false)  // Nullable - only for sending tickets
                  .ValueGeneratedNever();

            entity.Property(t => t.OperatorId)
                  .HasColumnName("operator_id")
                  .IsRequired()
                  .ValueGeneratedNever();

            entity.Property(t => t.ProductId)
                  .HasColumnName("product_id")
                  .IsRequired(false)
                  .ValueGeneratedNever();

            entity.Property(t => t.CurrencyId)
                  .HasColumnName("currency_id")
                  .IsRequired(false)
                  .ValueGeneratedNever();

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

            // Header ex-VAT total (DB column total_amount_ex_vat)
            entity.Property(t => t.TotalAmount)
                  .HasColumnName("total_amount_ex_vat")
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

            entity.Property(t => t.VehicleRegistration)
                  .HasColumnName("vehicle_registration")
                  .HasMaxLength(50);

            entity.Property(t => t.TrailerRegistration)
                  .HasColumnName("trailer_registration")
                  .HasMaxLength(50);

            entity.Property(t => t.DriverName)
                  .HasColumnName("driver_name")
                  .HasMaxLength(100);

            entity.Property(t => t.OfmWeighbridgeTicket)
                  .HasColumnName("ofm_weighbridge_ticket")
                  .HasMaxLength(50);

            entity.Property(t => t.ForeignTicket)
                  .HasColumnName("foreign_ticket")
                  .HasMaxLength(50);

            entity.Property(t => t.CkNumber)
                  .HasColumnName("ck_number")
                  .HasMaxLength(50);

            entity.Property(t => t.DeliveryNumber)
                  .HasColumnName("delivery_number")
                  .HasMaxLength(100);

            entity.Property(t => t.Status)
                  .HasColumnName("status")
                  .HasMaxLength(50)
                  .IsRequired()
                  .HasDefaultValue("receiving");

            entity.Property(t => t.RfidCardNumber)
                  .HasColumnName("rfid_card_number")
                  .HasMaxLength(100);

            entity.Property(t => t.VatRate)
                  .HasColumnName("vat_rate")
                  .HasColumnType("numeric(5,4)")
                  .IsRequired();

            entity.Property(t => t.VatAmount)
                  .HasColumnName("vat_amount")
                  .HasColumnType("numeric(18,2)")
                  .IsRequired();

            entity.Property(t => t.TotalInclVat)
                  .HasColumnName("total_incl_vat")
                  .HasColumnType("numeric(18,2)")
                  .IsRequired();

            entity.Property(t => t.IsActive)
                  .HasColumnName("is_active")
                  .IsRequired();

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

            entity.HasOne(t => t.Customer)
                  .WithMany(c => c.Tickets)  // IMPORTANT: Use the reverse navigation!
                  .HasForeignKey(t => t.CustomerId)
                  .HasConstraintName("fk_tickets_customer_id_customers")
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Buyer)
                  .WithMany(b => b.Tickets)  // Buyer → Tickets navigation
                  .HasForeignKey(t => t.BuyerId)
                  .HasConstraintName("fk_tickets_buyer_id_buyers")
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Operator)
                  .WithMany()  // Operator doesn't have reverse navigation
                  .HasForeignKey(t => t.OperatorId)
                  .HasConstraintName("fk_tickets_operator_id_operators")
                  .OnDelete(DeleteBehavior.Restrict);

            // Optional: FK to Site for referential integrity
            entity.HasOne(t => t.Site)
                  .WithMany()  // Site doesn't have reverse navigation
                  .HasForeignKey(t => t.SiteId)
                  .HasConstraintName("fk_tickets_site_id_sites")
                  .OnDelete(DeleteBehavior.Restrict);

            // Optional: FK to Product
            entity.HasOne(t => t.Product)
                  .WithMany()  // Product doesn't have reverse navigation
                  .HasForeignKey(t => t.ProductId)
                  .HasConstraintName("fk_tickets_product_id_products")
                  .OnDelete(DeleteBehavior.Restrict);

            // Optional: FK to Currency
            entity.HasOne(t => t.Currency)
                  .WithMany()  // Currency doesn't have reverse navigation
                  .HasForeignKey(t => t.CurrencyId)
                  .HasConstraintName("fk_tickets_currency_id_currencies")
                  .OnDelete(DeleteBehavior.Restrict);

            // Ticket ↔ TicketLines
            entity.HasMany(t => t.Lines)
                  .WithOne(l => l.Ticket)
                  .HasForeignKey(l => l.TicketId)
                  .HasConstraintName("fk_ticket_lines_ticket_id_tickets");
      }

      // -------------------------
      // TICKET LINE
      // -------------------------

      private static void ConfigureTicketLine(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<TicketLine>();

            entity.ToTable("ticket_lines", schema: "metal_link");

            entity.HasKey(l => l.TicketLineId)
                  .HasName("pk_ticket_lines_ticket_line_id");

            entity.Property(l => l.TicketLineId)
                  .HasColumnName("ticket_line_id")
                  .ValueGeneratedOnAdd();

            entity.Property(l => l.TicketId)
                  .HasColumnName("ticket_id")
                  .IsRequired();

            entity.Property(l => l.ProductId)
                  .HasColumnName("product_id")
                  .IsRequired();

            entity.Property(l => l.ProductName)
                  .HasColumnName("product_name")
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(l => l.WeightKg)
                  .HasColumnName("weight_kg")
                  .HasColumnType("numeric(18,3)")
                  .IsRequired();

            entity.Property(l => l.UnitPricePerKg)
                  .HasColumnName("unit_price_per_kg")
                  .HasColumnType("numeric(18,4)")
                  .IsRequired();

            entity.Property(l => l.LineTotal)
                  .HasColumnName("line_total")
                  .HasColumnType("numeric(18,2)")
                  .IsRequired();

            entity.Property(l => l.VatAmount)
                  .HasColumnName("vat_amount")
                  .HasColumnType("numeric(18,2)")
                  .IsRequired();

            entity.Property(l => l.TotalInclVat)
                  .HasColumnName("total_incl_vat")
                  .HasColumnType("numeric(18,2)")
                  .IsRequired();

            entity.Property(l => l.IsActive)
                  .HasColumnName("is_active")
                  .IsRequired();

            entity.Property(l => l.CreatedTime)
                  .HasColumnName("created_time")
                  .IsRequired();

            entity.Property(l => l.UpdatedTime)
                  .HasColumnName("updated_time")
                  .IsRequired();

            entity.HasIndex(l => l.TicketId)
                  .HasDatabaseName("ticket_lines_ticket_id_idx");

            entity.HasIndex(l => l.ProductId)
                  .HasDatabaseName("ticket_lines_product_id_idx");

            entity.HasOne(l => l.Ticket)
                  .WithMany(t => t.Lines)
                  .HasForeignKey(l => l.TicketId)
                  .HasConstraintName("fk_ticket_lines_ticket_id_tickets");

            entity.HasOne(l => l.Product)
                  .WithMany()
                  .HasForeignKey(l => l.ProductId)
                  .HasConstraintName("fk_ticket_lines_product_id_products");
      }

      // -------------------------
      // PRODUCT
      // -------------------------

      private static void ConfigureProduct(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<Product>();

            entity.ToTable("products", schema: "metal_link");

            entity.HasKey(p => p.ProductId)
                  .HasName("pk_products_product_id");

            entity.Property(p => p.ProductId)
                  .HasColumnName("product_id")
                  .ValueGeneratedOnAdd();

            entity.Property(p => p.ProductCode)
                  .HasColumnName("product_code")
                  .HasMaxLength(50);

            entity.Property(p => p.ProductName)
                  .HasColumnName("product_name")
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(p => p.Grade)
                  .HasColumnName("grade")
                  .HasColumnType("numeric(18,2)")
                  .HasDefaultValue(0m);

            entity.Property(p => p.IsActive)
                  .HasColumnName("is_active")
                  .IsRequired();

            entity.Property(p => p.CreatedTime)
                  .HasDefaultValueSql("now()")
                  .HasColumnName("created_time")
                  .ValueGeneratedOnAdd()
                  .IsRequired();

            entity.Property(p => p.UpdatedTime)
                  .HasDefaultValueSql("now()")
                  .HasColumnName("updated_time")
                  .ValueGeneratedOnAddOrUpdate()
                  .IsRequired();

            entity.HasIndex(p => p.ProductName)
                  .HasDatabaseName("products_product_name_idx");

            entity.HasIndex(p => p.ProductCode)
                  .HasDatabaseName("products_product_code_idx");
      }

      // -------------------------
      // PRICE
      // -------------------------

      private static void ConfigurePrice(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<Price>();

            entity.ToTable("prices", schema: "metal_link");

            entity.HasKey(p => p.PriceId)
                  .HasName("pk_prices_price_id");

            entity.Property(p => p.PriceId)
                  .HasColumnName("price_id")
                  .ValueGeneratedOnAdd();

            entity.Property(p => p.ProductId)
                  .HasColumnName("product_id")
                  .IsRequired();

            entity.Property(p => p.PriceA)
                  .HasColumnName("price_a")
                  .HasColumnType("numeric(18,2)")
                  .IsRequired();

            entity.Property(p => p.PriceB)
                  .HasColumnName("price_b")
                  .HasColumnType("numeric(18,2)")
                  .IsRequired();

            entity.Property(p => p.PriceC)
                  .HasColumnName("price_c")
                  .HasColumnType("numeric(18,2)")
                  .IsRequired();

            entity.Property(p => p.IsActive)
                  .HasColumnName("is_active")
                  .IsRequired();

            entity.Property(p => p.CreatedTime)
                  .HasDefaultValueSql("now()")
                  .HasColumnName("created_time")
                  .ValueGeneratedOnAdd()
                  .IsRequired();

            entity.Property(p => p.UpdatedTime)
                  .HasDefaultValueSql("now()")
                  .HasColumnName("updated_time")
                  .ValueGeneratedOnAddOrUpdate()
                  .IsRequired();

            entity.HasIndex(p => p.ProductId)
                  .HasDatabaseName("prices_product_id_idx");

            // FK → Product
            entity.HasOne(p => p.Product)
                  .WithMany()
                  .HasForeignKey(p => p.ProductId)
                  .HasConstraintName("fk_prices_product_id_products");
      }

      // -------------------------
      // BUYER
      // -------------------------

      private static void ConfigureBuyer(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<Buyer>();

            entity.ToTable("buyers", schema: "metal_link");

            entity.HasKey(b => b.BuyerId)
                  .HasName("pk_buyers_buyer_id");

            entity.Property(b => b.BuyerId)
                  .HasColumnName("buyer_id")
                  .ValueGeneratedOnAdd();

            entity.Property(b => b.CompanyId)
                  .HasColumnName("company_id")
                  .IsRequired(false);

            entity.Property(b => b.SiteId)
                  .HasColumnName("site_id")
                  .IsRequired(false);

            entity.Property(b => b.BuyerName)
                  .HasColumnName("buyer_name")
                  .HasMaxLength(255);

            entity.Property(b => b.ContactPerson)
                  .HasColumnName("contact_person")
                  .HasMaxLength(255);

            entity.Property(b => b.IsCompany)
                  .HasColumnName("is_company")
                  .IsRequired()
                  .HasDefaultValue(true);

            entity.Property(b => b.RegistrationNumber)
                  .HasColumnName("registration_number")
                  .HasMaxLength(100);

            entity.Property(b => b.VatNumber)
                  .HasColumnName("vat_number")
                  .HasMaxLength(100);

            entity.Property(b => b.AccountNumber)
                  .HasColumnName("account_number");

            entity.Property(b => b.PriceCode)
                  .HasColumnName("price_code")
                  .HasMaxLength(50);

            entity.Property(b => b.PhoneNumber)
                  .HasColumnName("phone_number")
                  .HasMaxLength(50);

            entity.Property(b => b.MobileNumber)
                  .HasColumnName("mobile_number")
                  .HasMaxLength(50);

            entity.Property(b => b.Email)
                  .HasColumnName("email")
                  .HasMaxLength(255);

            entity.Property(b => b.Address)
                  .HasColumnName("address");

            entity.Property(b => b.Taxable)
                  .HasColumnName("taxable")
                  .IsRequired()
                  .HasDefaultValue(true);

            entity.Property(b => b.PaymentTerms)
                  .HasColumnName("payment_terms")
                  .HasMaxLength(100);

            entity.Property(b => b.Notes)
                  .HasColumnName("notes");

            entity.Property(b => b.IsActive)
                  .HasColumnName("is_active")
                  .IsRequired()
                  .HasDefaultValue(true);

            entity.Property(b => b.CreatedTime)
                  .HasColumnName("created_time");

            entity.Property(b => b.UpdatedTime)
                  .HasColumnName("updated_time");

            entity.HasIndex(b => b.BuyerName)
                  .HasDatabaseName("buyers_buyer_name_idx");

            entity.HasIndex(b => b.AccountNumber)
                  .HasDatabaseName("buyers_account_number_idx");
      }

      // -------------------------
      // STOCK MOVEMENT
      // -------------------------

      private static void ConfigureStockMovement(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<StockMovement>();

            entity.ToTable("stock_movements", schema: "metal_link");

            entity.HasKey(sm => sm.StockMovementId)
                  .HasName("pk_stock_movements_stock_movement_id");

            entity.Property(sm => sm.StockMovementId)
                  .HasColumnName("stock_movement_id")
                  .ValueGeneratedOnAdd();

            entity.Property(sm => sm.SiteId)
                  .HasColumnName("site_id")
                  .IsRequired();

            entity.Property(sm => sm.ProductId)
                  .HasColumnName("product_id")
                  .IsRequired();

            entity.Property(sm => sm.TicketId)
                  .HasColumnName("ticket_id")
                  .IsRequired();

            entity.Property(sm => sm.TicketLineId)
                  .HasColumnName("ticket_line_id")
                  .IsRequired(false);

            entity.Property(sm => sm.MovementType)
                  .HasColumnName("movement_type")
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(sm => sm.QuantityKg)
                  .HasColumnName("quantity_kg")
                  .HasColumnType("numeric(18,2)")
                  .IsRequired();

            entity.Property(sm => sm.UnitPricePerKg)
                  .HasColumnName("unit_price_per_kg")
                  .HasColumnType("numeric(18,2)")
                  .IsRequired();

            entity.Property(sm => sm.CurrencyCode)
                  .HasColumnName("currency_code")
                  .IsRequired()
                  .HasMaxLength(10)
                  .HasDefaultValue("ZAR");

            entity.Property(sm => sm.ReferenceNumber)
                  .HasColumnName("reference_number")
                  .HasMaxLength(100);

            entity.Property(sm => sm.CounterpartyName)
                  .HasColumnName("counterparty_name")
                  .HasMaxLength(255);

            entity.Property(sm => sm.CounterpartyType)
                  .HasColumnName("counterparty_type")
                  .HasMaxLength(50);

            entity.Property(sm => sm.Notes)
                  .HasColumnName("notes");

            entity.Property(sm => sm.IsActive)
                  .HasColumnName("is_active")
                  .IsRequired()
                  .HasDefaultValue(true);

            entity.Property(sm => sm.CreatedTime)
                  .HasColumnName("created_time")
                  .IsRequired();

            entity.Property(sm => sm.UpdatedTime)
                  .HasColumnName("updated_time")
                  .IsRequired();

            entity.HasIndex(sm => sm.SiteId)
                  .HasDatabaseName("stock_movements_site_id_idx");

            entity.HasIndex(sm => sm.ProductId)
                  .HasDatabaseName("stock_movements_product_id_idx");

            entity.HasIndex(sm => sm.TicketId)
                  .HasDatabaseName("stock_movements_ticket_id_idx");

            entity.HasIndex(sm => sm.MovementType)
                  .HasDatabaseName("stock_movements_movement_type_idx");
      }

      // -------------------------
      // TICKET RECEIVING
      // -------------------------

      private static void ConfigureTicketReceiving(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<TicketReceiving>();
            entity.ToTable("tickets_receiving", schema: "metal_link");
            entity.HasKey(t => t.TicketReceivingId).HasName("pk_tickets_receiving");
            entity.Property(t => t.TicketReceivingId).HasColumnName("ticket_receiving_id").ValueGeneratedOnAdd();
            entity.Property(t => t.CompanyId).HasColumnName("company_id").IsRequired();
            entity.Property(t => t.SiteId).HasColumnName("site_id").IsRequired();
            entity.Property(t => t.CustomerId).HasColumnName("customer_id").IsRequired();
            entity.Property(t => t.TicketNumber).HasColumnName("ticket_number").IsRequired().HasMaxLength(100);
            entity.Property(t => t.TicketType).HasColumnName("ticket_type").IsRequired().HasMaxLength(50).HasDefaultValue("weighbridge");
            entity.Property(t => t.FirstWeightKg).HasColumnName("first_weight_kg").HasColumnType("numeric(18,2)");
            entity.Property(t => t.SecondWeightKg).HasColumnName("second_weight_kg").HasColumnType("numeric(18,2)");
            entity.Property(t => t.NetWeightKg).HasColumnName("net_weight_kg").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(t => t.UnitPricePerKg).HasColumnName("unit_price_per_kg").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(t => t.TotalAmount).HasColumnName("total_amount").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(t => t.CurrencyCode).HasColumnName("currency_code").IsRequired().HasMaxLength(10).HasDefaultValue("ZAR");
            entity.Property(t => t.ProductId).HasColumnName("product_id");
            entity.Property(t => t.ProductDescription).HasColumnName("product_description").HasMaxLength(500);
            entity.Property(t => t.VehicleRegistration).HasColumnName("vehicle_registration").HasMaxLength(50);
            entity.Property(t => t.TrailerRegistration).HasColumnName("trailer_registration").HasMaxLength(50);
            entity.Property(t => t.DriverName).HasColumnName("driver_name").HasMaxLength(255);
            entity.Property(t => t.OfmWeighbridgeTicket).HasColumnName("ofm_weighbridge_ticket").HasMaxLength(100);
            entity.Property(t => t.ForeignTicket).HasColumnName("foreign_ticket").HasMaxLength(100);
            entity.Property(t => t.CkNumber).HasColumnName("ck_number").HasMaxLength(100);
            entity.Property(t => t.DeliveryNumber).HasColumnName("delivery_number").HasMaxLength(100);
            entity.Property(t => t.RfidTag).HasColumnName("rfid_tag").HasMaxLength(100);
            entity.Property(t => t.RfidFirstScan).HasColumnName("rfid_first_scan");
            entity.Property(t => t.RfidSecondScan).HasColumnName("rfid_second_scan");
            entity.Property(t => t.DeliveryStatus).HasColumnName("delivery_status").IsRequired().HasMaxLength(50).HasDefaultValue("pending");
            entity.Property(t => t.Notes).HasColumnName("notes");
            entity.Property(t => t.PlatePhotoUrl).HasColumnName("plate_photo_url").HasMaxLength(500);
            entity.Property(t => t.LoadPhotoUrl).HasColumnName("load_photo_url").HasMaxLength(500);
            entity.Property(t => t.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
            entity.Property(t => t.CreatedTime).HasColumnName("created_time").IsRequired();
            entity.Property(t => t.UpdatedTime).HasColumnName("updated_time").IsRequired();
            entity.Property(t => t.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
            entity.Property(t => t.UpdatedByOperatorId).HasColumnName("updated_by_operator_id");
            
            entity.HasOne(t => t.Company).WithMany().HasForeignKey(t => t.CompanyId);
            entity.HasOne(t => t.Site).WithMany().HasForeignKey(t => t.SiteId);
            entity.HasOne(t => t.Customer).WithMany().HasForeignKey(t => t.CustomerId);
            entity.HasOne(t => t.Product).WithMany().HasForeignKey(t => t.ProductId);
            
            entity.HasIndex(t => t.TicketNumber).IsUnique().HasDatabaseName("tickets_receiving_ticket_number_idx");
            entity.HasIndex(t => t.CustomerId).HasDatabaseName("tickets_receiving_customer_id_idx");
            entity.HasIndex(t => t.SiteId).HasDatabaseName("tickets_receiving_site_id_idx");
            entity.HasIndex(t => t.CreatedTime).HasDatabaseName("tickets_receiving_created_time_idx");
      }

      private static void ConfigureTicketReceivingLine(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<TicketReceivingLine>();
            entity.ToTable("ticket_receiving_lines", schema: "metal_link");
            entity.HasKey(l => l.TicketReceivingLineId).HasName("pk_ticket_receiving_lines");
            entity.Property(l => l.TicketReceivingLineId).HasColumnName("ticket_receiving_line_id").ValueGeneratedOnAdd();
            entity.Property(l => l.TicketReceivingId).HasColumnName("ticket_receiving_id").IsRequired();
            entity.Property(l => l.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(l => l.WeightKg).HasColumnName("weight_kg").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(l => l.UnitPricePerKg).HasColumnName("unit_price_per_kg").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(l => l.LineTotal).HasColumnName("line_total").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(l => l.Notes).HasColumnName("notes");
            entity.Property(l => l.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
            entity.Property(l => l.CreatedTime).HasColumnName("created_time").IsRequired();
            
            entity.HasOne(l => l.TicketReceiving).WithMany(t => t.Lines).HasForeignKey(l => l.TicketReceivingId);
            entity.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId);
            
            entity.HasIndex(l => l.TicketReceivingId).HasDatabaseName("ticket_receiving_lines_ticket_receiving_id_idx");
      }

      // -------------------------
      // TICKET SENDING
      // -------------------------

      private static void ConfigureTicketSending(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<TicketSending>();
            entity.ToTable("tickets_sending", schema: "metal_link");
            entity.HasKey(t => t.TicketSendingId).HasName("pk_tickets_sending");
            entity.Property(t => t.TicketSendingId).HasColumnName("ticket_sending_id").ValueGeneratedOnAdd();
            entity.Property(t => t.CompanyId).HasColumnName("company_id").IsRequired();
            entity.Property(t => t.SiteId).HasColumnName("site_id").IsRequired();
            entity.Property(t => t.BuyerId).HasColumnName("buyer_id").IsRequired();
            entity.Property(t => t.TicketNumber).HasColumnName("ticket_number").IsRequired().HasMaxLength(100);
            entity.Property(t => t.TicketType).HasColumnName("ticket_type").IsRequired().HasMaxLength(50).HasDefaultValue("weighbridge");
            entity.Property(t => t.FirstWeightKg).HasColumnName("first_weight_kg").HasColumnType("numeric(18,2)");
            entity.Property(t => t.SecondWeightKg).HasColumnName("second_weight_kg").HasColumnType("numeric(18,2)");
            entity.Property(t => t.NetWeightKg).HasColumnName("net_weight_kg").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(t => t.UnitPricePerKg).HasColumnName("unit_price_per_kg").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(t => t.TotalAmount).HasColumnName("total_amount").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(t => t.CurrencyCode).HasColumnName("currency_code").IsRequired().HasMaxLength(10).HasDefaultValue("ZAR");
            entity.Property(t => t.ProductId).HasColumnName("product_id");
            entity.Property(t => t.ProductDescription).HasColumnName("product_description").HasMaxLength(500);
            entity.Property(t => t.VehicleRegistration).HasColumnName("vehicle_registration").HasMaxLength(50);
            entity.Property(t => t.TrailerRegistration).HasColumnName("trailer_registration").HasMaxLength(50);
            entity.Property(t => t.DriverName).HasColumnName("driver_name").HasMaxLength(255);
            entity.Property(t => t.OfmWeighbridgeTicket).HasColumnName("ofm_weighbridge_ticket").HasMaxLength(100);
            entity.Property(t => t.ForeignTicket).HasColumnName("foreign_ticket").HasMaxLength(100);
            entity.Property(t => t.CkNumber).HasColumnName("ck_number").HasMaxLength(100);
            entity.Property(t => t.DeliveryNumber).HasColumnName("delivery_number").HasMaxLength(100);
            entity.Property(t => t.RfidTag).HasColumnName("rfid_tag").HasMaxLength(100);
            entity.Property(t => t.RfidFirstScan).HasColumnName("rfid_first_scan");
            entity.Property(t => t.RfidSecondScan).HasColumnName("rfid_second_scan");
            entity.Property(t => t.DeliveryStatus).HasColumnName("delivery_status").IsRequired().HasMaxLength(50).HasDefaultValue("pending");
            entity.Property(t => t.Notes).HasColumnName("notes");
            entity.Property(t => t.PlatePhotoUrl).HasColumnName("plate_photo_url").HasMaxLength(500);
            entity.Property(t => t.LoadPhotoUrl).HasColumnName("load_photo_url").HasMaxLength(500);
            entity.Property(t => t.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
            entity.Property(t => t.CreatedTime).HasColumnName("created_time").IsRequired();
            entity.Property(t => t.UpdatedTime).HasColumnName("updated_time").IsRequired();
            entity.Property(t => t.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
            entity.Property(t => t.UpdatedByOperatorId).HasColumnName("updated_by_operator_id");
            
            entity.HasOne(t => t.Company).WithMany().HasForeignKey(t => t.CompanyId);
            entity.HasOne(t => t.Site).WithMany().HasForeignKey(t => t.SiteId);
            entity.HasOne(t => t.Buyer).WithMany().HasForeignKey(t => t.BuyerId);
            entity.HasOne(t => t.Product).WithMany().HasForeignKey(t => t.ProductId);
            
            entity.HasIndex(t => t.TicketNumber).IsUnique().HasDatabaseName("tickets_sending_ticket_number_idx");
            entity.HasIndex(t => t.BuyerId).HasDatabaseName("tickets_sending_buyer_id_idx");
            entity.HasIndex(t => t.SiteId).HasDatabaseName("tickets_sending_site_id_idx");
            entity.HasIndex(t => t.CreatedTime).HasDatabaseName("tickets_sending_created_time_idx");
      }

      private static void ConfigureTicketSendingLine(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<TicketSendingLine>();
            entity.ToTable("ticket_sending_lines", schema: "metal_link");
            entity.HasKey(l => l.TicketSendingLineId).HasName("pk_ticket_sending_lines");
            entity.Property(l => l.TicketSendingLineId).HasColumnName("ticket_sending_line_id").ValueGeneratedOnAdd();
            entity.Property(l => l.TicketSendingId).HasColumnName("ticket_sending_id").IsRequired();
            entity.Property(l => l.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(l => l.WeightKg).HasColumnName("weight_kg").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(l => l.UnitPricePerKg).HasColumnName("unit_price_per_kg").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(l => l.LineTotal).HasColumnName("line_total").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(l => l.Notes).HasColumnName("notes");
            entity.Property(l => l.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
            entity.Property(l => l.CreatedTime).HasColumnName("created_time").IsRequired();
            
            entity.HasOne(l => l.TicketSending).WithMany(t => t.Lines).HasForeignKey(l => l.TicketSendingId);
            entity.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId);
            
            entity.HasIndex(l => l.TicketSendingId).HasDatabaseName("ticket_sending_lines_ticket_sending_id_idx");
      }

      // -------------------------
      // STOCK MOVEMENTS (Receiving/Sending)
      // -------------------------

      private static void ConfigureStockMovementReceiving(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<StockMovementReceiving>();
            entity.ToTable("stock_movements_receiving", schema: "metal_link");
            entity.HasKey(sm => sm.StockMovementReceivingId).HasName("pk_stock_movements_receiving");
            entity.Property(sm => sm.StockMovementReceivingId).HasColumnName("stock_movement_receiving_id").ValueGeneratedOnAdd();
            entity.Property(sm => sm.SiteId).HasColumnName("site_id").IsRequired();
            entity.Property(sm => sm.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(sm => sm.TicketReceivingId).HasColumnName("ticket_receiving_id").IsRequired();
            entity.Property(sm => sm.TicketReceivingLineId).HasColumnName("ticket_receiving_line_id");
            entity.Property(sm => sm.QuantityKg).HasColumnName("quantity_kg").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(sm => sm.UnitPricePerKg).HasColumnName("unit_price_per_kg").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(sm => sm.TotalValue).HasColumnName("total_value").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(sm => sm.CurrencyCode).HasColumnName("currency_code").IsRequired().HasMaxLength(10).HasDefaultValue("ZAR");
            entity.Property(sm => sm.TicketNumber).HasColumnName("ticket_number").IsRequired().HasMaxLength(100);
            entity.Property(sm => sm.CustomerId).HasColumnName("customer_id").IsRequired();
            entity.Property(sm => sm.CustomerName).HasColumnName("customer_name").IsRequired().HasMaxLength(255);
            entity.Property(sm => sm.Notes).HasColumnName("notes");
            entity.Property(sm => sm.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
            entity.Property(sm => sm.MovementDate).HasColumnName("movement_date").IsRequired();
            entity.Property(sm => sm.CreatedTime).HasColumnName("created_time").IsRequired();
            entity.Property(sm => sm.UpdatedTime).HasColumnName("updated_time").IsRequired();
            
            entity.HasOne(sm => sm.Site).WithMany().HasForeignKey(sm => sm.SiteId);
            entity.HasOne(sm => sm.Product).WithMany().HasForeignKey(sm => sm.ProductId);
            entity.HasOne(sm => sm.TicketReceiving).WithMany(t => t.StockMovements).HasForeignKey(sm => sm.TicketReceivingId);
            
            entity.HasIndex(sm => sm.SiteId).HasDatabaseName("stock_movements_receiving_site_id_idx");
            entity.HasIndex(sm => sm.ProductId).HasDatabaseName("stock_movements_receiving_product_id_idx");
            entity.HasIndex(sm => sm.MovementDate).HasDatabaseName("stock_movements_receiving_movement_date_idx");
      }

      private static void ConfigureStockMovementSending(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<StockMovementSending>();
            entity.ToTable("stock_movements_sending", schema: "metal_link");
            entity.HasKey(sm => sm.StockMovementSendingId).HasName("pk_stock_movements_sending");
            entity.Property(sm => sm.StockMovementSendingId).HasColumnName("stock_movement_sending_id").ValueGeneratedOnAdd();
            entity.Property(sm => sm.SiteId).HasColumnName("site_id").IsRequired();
            entity.Property(sm => sm.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(sm => sm.TicketSendingId).HasColumnName("ticket_sending_id").IsRequired();
            entity.Property(sm => sm.TicketSendingLineId).HasColumnName("ticket_sending_line_id");
            entity.Property(sm => sm.QuantityKg).HasColumnName("quantity_kg").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(sm => sm.UnitPricePerKg).HasColumnName("unit_price_per_kg").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(sm => sm.TotalValue).HasColumnName("total_value").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(sm => sm.CurrencyCode).HasColumnName("currency_code").IsRequired().HasMaxLength(10).HasDefaultValue("ZAR");
            entity.Property(sm => sm.TicketNumber).HasColumnName("ticket_number").IsRequired().HasMaxLength(100);
            entity.Property(sm => sm.BuyerId).HasColumnName("buyer_id").IsRequired();
            entity.Property(sm => sm.BuyerName).HasColumnName("buyer_name").IsRequired().HasMaxLength(255);
            entity.Property(sm => sm.Notes).HasColumnName("notes");
            entity.Property(sm => sm.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
            entity.Property(sm => sm.MovementDate).HasColumnName("movement_date").IsRequired();
            entity.Property(sm => sm.CreatedTime).HasColumnName("created_time").IsRequired();
            entity.Property(sm => sm.UpdatedTime).HasColumnName("updated_time").IsRequired();
            
            entity.HasOne(sm => sm.Site).WithMany().HasForeignKey(sm => sm.SiteId);
            entity.HasOne(sm => sm.Product).WithMany().HasForeignKey(sm => sm.ProductId);
            entity.HasOne(sm => sm.TicketSending).WithMany(t => t.StockMovements).HasForeignKey(sm => sm.TicketSendingId);
            
            entity.HasIndex(sm => sm.SiteId).HasDatabaseName("stock_movements_sending_site_id_idx");
            entity.HasIndex(sm => sm.ProductId).HasDatabaseName("stock_movements_sending_product_id_idx");
            entity.HasIndex(sm => sm.MovementDate).HasDatabaseName("stock_movements_sending_movement_date_idx");
      }

      private static void ConfigureStockOnHand(ModelBuilder modelBuilder)
      {
            var entity = modelBuilder.Entity<StockOnHand>();
            entity.ToTable("stock_on_hand", schema: "metal_link");
            entity.HasKey(s => s.StockOnHandId).HasName("pk_stock_on_hand");
            entity.Property(s => s.StockOnHandId).HasColumnName("stock_on_hand_id").ValueGeneratedOnAdd();
            entity.Property(s => s.SiteId).HasColumnName("site_id").IsRequired();
            entity.Property(s => s.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(s => s.QuantityOnHandKg).HasColumnName("quantity_on_hand_kg").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(s => s.TotalReceivedKg).HasColumnName("total_received_kg").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(s => s.TotalSentKg).HasColumnName("total_sent_kg").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(s => s.AverageUnitCost).HasColumnName("average_unit_cost").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(s => s.TotalValue).HasColumnName("total_value").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(s => s.LastMovementDate).HasColumnName("last_movement_date");
            entity.Property(s => s.LastMovementType).HasColumnName("last_movement_type").HasMaxLength(50);
            entity.Property(s => s.CreatedTime).HasColumnName("created_time").IsRequired();
            entity.Property(s => s.UpdatedTime).HasColumnName("updated_time").IsRequired();
            
            entity.HasOne(s => s.Site).WithMany().HasForeignKey(s => s.SiteId);
            entity.HasOne(s => s.Product).WithMany().HasForeignKey(s => s.ProductId);
            
            entity.HasIndex(s => new { s.SiteId, s.ProductId }).IsUnique().HasDatabaseName("stock_on_hand_site_product_idx");
      }
}
