using Microsoft.EntityFrameworkCore;
using MetalLink.Domain.Entities;
using System.Linq.Expressions;

namespace MetalLink.Infrastructure.Persistence;

public class MetalLinkDbContext : DbContext
{
    public MetalLinkDbContext(DbContextOptions<MetalLinkDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // Register the audit interceptor to auto-update UpdatedTime on all changes
        optionsBuilder.AddInterceptors(new AuditInterceptor());
        
        // Log all SQL commands to console
        optionsBuilder.LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name });
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Currency> Currencies => Set<Currency>();

    public DbSet<ImagePath> ImagePaths => Set<ImagePath>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Buyer> Buyers => Set<Buyer>();
    public DbSet<Operator> Operators => Set<Operator>();

    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<SettingOption> SettingOptions => Set<SettingOption>();
    public DbSet<OperatorSetting> OperatorSettings => Set<OperatorSetting>();

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Price> Prices => Set<Price>();

    public DbSet<TicketType> TicketTypes => Set<TicketType>();

    public DbSet<TicketReceiving> ReceivingTickets => Set<TicketReceiving>();
    public DbSet<TicketReceivingLine> ReceivingTicketLines => Set<TicketReceivingLine>();
    public DbSet<TicketSending> SendingTickets => Set<TicketSending>();
    public DbSet<TicketSendingLine> SendingTicketLines => Set<TicketSendingLine>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCompanies(modelBuilder);
        ConfigureSites(modelBuilder);
        ConfigureProvinces(modelBuilder);
        ConfigureCountries(modelBuilder);
        ConfigureCurrencies(modelBuilder);

        ConfigureImagePaths(modelBuilder);
        ConfigureCustomers(modelBuilder);
        ConfigureBuyers(modelBuilder);
        ConfigureOperators(modelBuilder);
        ConfigureSettings(modelBuilder);
        ConfigureSettingOptions(modelBuilder);
        ConfigureOperatorSettings(modelBuilder);

        ConfigureProducts(modelBuilder);
        ConfigurePrices(modelBuilder);

        ConfigureTicketTypes(modelBuilder);
        ConfigureReceivingTickets(modelBuilder);
        ConfigureReceivingTicketLines(modelBuilder);
        ConfigureSendingTickets(modelBuilder);
        ConfigureSendingTicketLines(modelBuilder);

        // Global soft-delete filter: automatically apply WHERE is_active = true
        // for all entities that have an IsActive boolean property.
        ApplyIsActiveGlobalQueryFilter(modelBuilder);
    }

    private static void ConfigureCompanies(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<Company>();
        e.ToTable("companies", "metal_link");
        e.HasKey(x => x.CompanyId);
        e.Property(x => x.CompanyId).HasColumnName("company_id").ValueGeneratedOnAdd();
        e.Property(x => x.CompanyName).HasColumnName("company_name").HasMaxLength(255).IsRequired();
        e.Property(x => x.VatNumber).HasColumnName("vat_number").HasMaxLength(50);
        e.Property(x => x.ReceivingSendingFlag).HasColumnName("receiving_sending_flag");
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");
    }

    private static void ConfigureSites(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<Site>();
        e.ToTable("sites", "metal_link");
        e.HasKey(x => x.SiteId);
        e.Property(x => x.SiteId).HasColumnName("site_id").ValueGeneratedOnAdd();
        e.Property(x => x.CompanyId).HasColumnName("company_id");
        e.Property(x => x.SiteName).HasColumnName("site_name").HasMaxLength(255).IsRequired();
        e.Property(x => x.SiteCode).HasColumnName("site_code").HasMaxLength(50);
        e.Property(x => x.AddressLine1).HasColumnName("address_line1").HasMaxLength(255);
        e.Property(x => x.AddressLine2).HasColumnName("address_line2").HasMaxLength(255);
        e.Property(x => x.Suburb).HasColumnName("suburb").HasMaxLength(255);
        e.Property(x => x.City).HasColumnName("city").HasMaxLength(255);
        e.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
        e.Property(x => x.ProvinceId).HasColumnName("province_id").IsRequired();
        e.Property(x => x.CountryId).HasColumnName("country_id").IsRequired();
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");

        e.HasOne(x => x.Company).WithMany(x => x.Sites).HasForeignKey(x => x.CompanyId);
        e.HasOne(x => x.Province).WithMany(x => x.Sites).HasForeignKey(x => x.ProvinceId);
        e.HasOne(x => x.Country).WithMany(x => x.Sites).HasForeignKey(x => x.CountryId);
    }

    private static void ConfigureProvinces(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<Province>();
        e.ToTable("provinces", "metal_link");
        e.HasKey(x => x.ProvinceId);
        e.Property(x => x.ProvinceId).HasColumnName("province_id").ValueGeneratedOnAdd();
        e.Property(x => x.ProvinceCode).HasColumnName("province_code").HasMaxLength(10);
        e.Property(x => x.ProvinceName).HasColumnName("province_name").HasMaxLength(255).IsRequired();
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");
    }

    private static void ConfigureCountries(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<Country>();
        e.ToTable("countries", "metal_link");
        e.HasKey(x => x.CountryId);
        e.Property(x => x.CountryId).HasColumnName("country_id").ValueGeneratedOnAdd();
        e.Property(x => x.CountryCode).HasColumnName("country_code").HasMaxLength(10);
        e.Property(x => x.CountryName).HasColumnName("country_name").HasMaxLength(255).IsRequired();
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");
    }

    private static void ConfigureCurrencies(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<Currency>();
        e.ToTable("currencies", "metal_link");
        e.HasKey(x => x.CurrencyId);
        e.Property(x => x.CurrencyId).HasColumnName("currency_id").ValueGeneratedNever();
        e.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10).IsRequired();
        e.Property(x => x.CurrencyName).HasColumnName("currency_name").HasMaxLength(255).IsRequired();
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");
    }

    private static void ConfigureImagePaths(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<ImagePath>();
        e.ToTable("image_paths", "metal_link");
        e.HasKey(x => x.ImagePathId);
        e.Property(x => x.ImagePathId).HasColumnName("image_path_id").ValueGeneratedOnAdd();
        e.Property(x => x.IdCardImagePath).HasColumnName("id_card_image_path");
        e.Property(x => x.DriverLicenseImagePath).HasColumnName("driver_license_image_path");
        e.Property(x => x.PhotoImagePath).HasColumnName("photo_image_path");
        e.Property(x => x.SignatureImagePath).HasColumnName("signature_image_path");
        e.Property(x => x.FingerprintImagePath).HasColumnName("fingerprint_image_path");
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");
    }

    private static void ConfigureCustomers(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<Customer>();
        e.ToTable("customers", "metal_link");
        e.HasKey(x => x.CustomerId);
        e.Property(x => x.CustomerId).HasColumnName("customer_id").ValueGeneratedOnAdd();
        e.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(255);
        e.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(255);
        e.Property(x => x.IdNumber).HasColumnName("id_number").HasMaxLength(20);
        e.Property(x => x.AccountNumber).HasColumnName("account_number");
        e.Property(x => x.IsCompany).HasColumnName("is_company");
        e.Property(x => x.CompanyId).HasColumnName("company_id");
        e.Property(x => x.SiteId).HasColumnName("site_id");
        e.Property(x => x.IsTaxable).HasColumnName("is_taxable");
        e.Property(x => x.PriceCode).HasColumnName("price_code").HasMaxLength(10);
        e.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20);
        e.Property(x => x.MobileNumber).HasColumnName("mobile_number").HasMaxLength(20);
        e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
        e.Property(x => x.ImagePathId).HasColumnName("image_path_id");
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");

        e.HasOne(x => x.Company).WithMany(x => x.Customers).HasForeignKey(x => x.CompanyId);
        e.HasOne(x => x.Site).WithMany(x => x.Customers).HasForeignKey(x => x.SiteId);
        e.HasOne(x => x.ImagePath).WithMany().HasForeignKey(x => x.ImagePathId);
    }

    private static void ConfigureBuyers(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<Buyer>();
        e.ToTable("buyers", "metal_link");
        e.HasKey(x => x.BuyerId);
        e.Property(x => x.BuyerId).HasColumnName("buyer_id").ValueGeneratedOnAdd();
        e.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(255);
        e.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(255);
        e.Property(x => x.IdNumber).HasColumnName("id_number").HasMaxLength(20);
        e.Property(x => x.AccountNumber).HasColumnName("account_number");
        e.Property(x => x.CompanyId).HasColumnName("company_id").IsRequired();
        e.Property(x => x.SiteId).HasColumnName("site_id").IsRequired();
        e.Property(x => x.IsTaxable).HasColumnName("is_taxable");
        e.Property(x => x.PriceCode).HasColumnName("price_code").HasMaxLength(10);
        e.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20);
        e.Property(x => x.MobileNumber).HasColumnName("mobile_number").HasMaxLength(20);
        e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
        e.Property(x => x.ImagePathId).HasColumnName("image_path_id");
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");

        e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
        e.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId);
        e.HasOne(x => x.ImagePath).WithMany().HasForeignKey(x => x.ImagePathId);
    }

    private static void ConfigureOperators(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<Operator>();
        e.ToTable("operators", "metal_link");
        e.HasKey(x => x.OperatorId);
        e.Property(x => x.OperatorId).HasColumnName("operator_id").ValueGeneratedOnAdd();
        e.Property(x => x.Username).HasColumnName("username").HasMaxLength(255).IsRequired();
        e.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(255).IsRequired();
        e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
        e.Property(x => x.Role).HasColumnName("role").HasMaxLength(50).IsRequired();
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");
    }

    private static void ConfigureSettings(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<Setting>();
        e.ToTable("settings", "metal_link");
        e.HasKey(x => x.SettingId);
        e.Property(x => x.SettingId).HasColumnName("setting_id").ValueGeneratedOnAdd();
        e.Property(x => x.SettingName).HasColumnName("setting_name").HasMaxLength(255).IsRequired();
        e.Property(x => x.SettingDescription).HasColumnName("setting_description").HasMaxLength(500);
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.TimeCreated).HasColumnName("time_created").HasDefaultValueSql("now()");
        e.Property(x => x.TimeUpdated).HasColumnName("time_updated").HasDefaultValueSql("now()");

        e.HasIndex(x => x.SettingName)
            .HasDatabaseName("settings_setting_name_active_idx")
            .IsUnique()
            .HasFilter("is_active = true");
        e.HasOne(x => x.CreatedByOperator).WithMany().HasForeignKey(x => x.CreatedByOperatorId);
    }

    private static void ConfigureSettingOptions(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<SettingOption>();
        e.ToTable("setting_options", "metal_link");
        e.HasKey(x => x.SettingOptionId);
        e.Property(x => x.SettingOptionId).HasColumnName("setting_option_id").ValueGeneratedOnAdd();
        e.Property(x => x.SettingId).HasColumnName("setting_id").IsRequired();
        e.Property(x => x.SettingOptionValue).HasColumnName("setting_option_value").HasMaxLength(255).IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.TimeCreated).HasColumnName("time_created").HasDefaultValueSql("now()");
        e.Property(x => x.TimeUpdated).HasColumnName("time_updated").HasDefaultValueSql("now()");

        e.HasIndex(x => new { x.SettingId, x.SettingOptionValue })
            .HasDatabaseName("setting_options_setting_id_value_active_idx")
            .IsUnique()
            .HasFilter("is_active = true");

        e.HasOne(x => x.Setting).WithMany(x => x.Options).HasForeignKey(x => x.SettingId);
        e.HasOne(x => x.CreatedByOperator).WithMany().HasForeignKey(x => x.CreatedByOperatorId);
    }

    private static void ConfigureOperatorSettings(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<OperatorSetting>();
        e.ToTable("operator_settings", "metal_link");
        e.HasKey(x => x.OperatorSettingId);
        e.Property(x => x.OperatorSettingId).HasColumnName("operator_setting_id").ValueGeneratedOnAdd();
        e.Property(x => x.OperatorId).HasColumnName("operator_id").IsRequired();
        e.Property(x => x.SettingId).HasColumnName("setting_id").IsRequired();
        e.Property(x => x.SettingOptionId).HasColumnName("setting_option_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.TimeCreated).HasColumnName("time_created").HasDefaultValueSql("now()");
        e.Property(x => x.TimeUpdated).HasColumnName("time_updated").HasDefaultValueSql("now()");

        e.HasIndex(x => new { x.OperatorId, x.SettingId })
            .HasDatabaseName("operator_settings_operator_setting_active_idx")
            .IsUnique()
            .HasFilter("is_active = true");

        e.HasOne(x => x.Operator).WithMany().HasForeignKey(x => x.OperatorId);
        e.HasOne(x => x.Setting).WithMany().HasForeignKey(x => x.SettingId);
        e.HasOne(x => x.SettingOption).WithMany().HasForeignKey(x => x.SettingOptionId);
        e.HasOne(x => x.CreatedByOperator).WithMany().HasForeignKey(x => x.CreatedByOperatorId);
    }

    private static void ConfigureProducts(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<Product>();
        e.ToTable("products", "metal_link");
        e.HasKey(x => x.ProductId);
        e.Property(x => x.ProductId).HasColumnName("product_id").ValueGeneratedOnAdd();
        e.Property(x => x.ProductCode).HasColumnName("product_code").HasMaxLength(50).IsRequired();
        e.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(255).IsRequired();
        e.Property(x => x.Grade).HasColumnName("grade").HasMaxLength(50);
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");
    }

    private static void ConfigurePrices(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<Price>();
        e.ToTable("prices", "metal_link");
        e.HasKey(x => x.PriceId);
        e.Property(x => x.PriceId).HasColumnName("price_id").ValueGeneratedOnAdd();
        e.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        e.Property(x => x.PriceA).HasColumnName("price_a");
        e.Property(x => x.PriceB).HasColumnName("price_b");
        e.Property(x => x.PriceC).HasColumnName("price_c");
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");

        e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
    }

    private static void ConfigureTicketTypes(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<TicketType>();
        e.ToTable("ticket_types", "metal_link");
        e.HasKey(x => x.TicketTypeId);
        e.Property(x => x.TicketTypeId).HasColumnName("ticket_type_id").ValueGeneratedNever();
        e.Property(x => x.TicketTypeName).HasColumnName("ticket_type_name").HasMaxLength(50).IsRequired();
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");
    }

    private static void ConfigureReceivingTickets(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<TicketReceiving>();
        e.ToTable("receiving_tickets", "metal_link");
        e.HasKey(x => x.TicketReceivingId);
        e.Property(x => x.TicketReceivingId).HasColumnName("receiving_ticket_id").ValueGeneratedOnAdd();
        e.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        e.Property(x => x.InvoiceNumber).HasColumnName("invoice_number");
        e.Property(x => x.TicketTypeId).HasColumnName("ticket_type_id").IsRequired();
        e.Property(x => x.TicketNumber).HasColumnName("ticket_number").HasMaxLength(100).IsRequired();
        e.Property(x => x.NetWeightKg).HasColumnName("net_weight_kg").IsRequired();
        e.Property(x => x.InitializeWeightKg).HasColumnName("initialize_weight_kg");
        e.Property(x => x.TicketState).HasColumnName("ticket_state").HasMaxLength(1).HasDefaultValue('C');
        e.Property(x => x.DriverName).HasColumnName("driver_name");
        e.Property(x => x.VehicleRegistration).HasColumnName("vehicle_registration");
        e.Property(x => x.TrailerRegistration).HasColumnName("trailer_registration");
        e.Property(x => x.Notes).HasColumnName("notes");
        e.Property(x => x.OfmWeighbridgeTicket).HasColumnName("ofm_weighbridge_ticket");
        e.Property(x => x.CkNumber).HasColumnName("ck_number");
        e.Property(x => x.DeliveryNumber).HasColumnName("delivery_number");
        e.Property(x => x.ForeignTicket).HasColumnName("foreign_ticket");
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");

        e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        e.HasOne(x => x.TicketType).WithMany(x => x.TicketsReceiving).HasForeignKey(x => x.TicketTypeId);
        e.HasOne(x => x.CreatedByOperator).WithMany().HasForeignKey(x => x.CreatedByOperatorId);
    }

    private static void ConfigureReceivingTicketLines(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<TicketReceivingLine>();
        e.ToTable("receiving_ticket_lines", "metal_link");
        e.HasKey(x => x.ReceivingTicketLineId);
        e.Property(x => x.ReceivingTicketLineId).HasColumnName("receiving_ticket_line_id").ValueGeneratedOnAdd();
        e.Property(x => x.ReceivingTicketId).HasColumnName("receiving_ticket_id").IsRequired();
        e.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        e.Property(x => x.FirstWeightKg).HasColumnName("first_weight_kg");
        e.Property(x => x.SecondWeightKg).HasColumnName("second_weight_kg");
        e.Property(x => x.NetWeightKg).HasColumnName("net_weight_kg").IsRequired();
        e.Property(x => x.UnitPricePerKg).HasColumnName("unit_price_per_kg").IsRequired();
        e.Property(x => x.Tare).HasColumnName("tare").HasDefaultValue(0m);
        e.Property(x => x.Notes).HasColumnName("notes");
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");

        e.HasOne(x => x.TicketReceiving).WithMany(x => x.Lines).HasForeignKey(x => x.ReceivingTicketId);
        e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
        e.HasOne(x => x.CreatedByOperator).WithMany().HasForeignKey(x => x.CreatedByOperatorId);
    }

    private static void ConfigureSendingTickets(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<TicketSending>();
        e.ToTable("sending_tickets", "metal_link");
        e.HasKey(x => x.TicketSendingId);
        e.Property(x => x.TicketSendingId).HasColumnName("sending_ticket_id").ValueGeneratedOnAdd();
        e.Property(x => x.BuyerId).HasColumnName("buyer_id").IsRequired();
        e.Property(x => x.InvoiceNumber).HasColumnName("invoice_number");
        e.Property(x => x.TicketTypeId).HasColumnName("ticket_type_id").IsRequired();
        e.Property(x => x.TicketNumber).HasColumnName("ticket_number").HasMaxLength(100).IsRequired();
        e.Property(x => x.NetWeightKg).HasColumnName("net_weight_kg").IsRequired();
        e.Property(x => x.InitializeWeightKg).HasColumnName("initialize_weight_kg");
        e.Property(x => x.TicketState).HasColumnName("ticket_state").HasMaxLength(1).HasDefaultValue('H');
        e.Property(x => x.DriverName).HasColumnName("driver_name");
        e.Property(x => x.VehicleRegistration).HasColumnName("vehicle_registration");
        e.Property(x => x.TrailerRegistration).HasColumnName("trailer_registration");
        e.Property(x => x.Notes).HasColumnName("notes");
        e.Property(x => x.OfmWeighbridgeTicket).HasColumnName("ofm_weighbridge_ticket");
        e.Property(x => x.CkNumber).HasColumnName("ck_number");
        e.Property(x => x.DeliveryNumber).HasColumnName("delivery_number");
        e.Property(x => x.ForeignTicket).HasColumnName("foreign_ticket");
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");

        e.HasOne(x => x.Buyer).WithMany(x => x.TicketsSending).HasForeignKey(x => x.BuyerId);
        e.HasOne(x => x.TicketType).WithMany(x => x.TicketsSending).HasForeignKey(x => x.TicketTypeId);
        e.HasOne(x => x.CreatedByOperator).WithMany().HasForeignKey(x => x.CreatedByOperatorId);
    }

    private static void ApplyIsActiveGlobalQueryFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Only apply to CLR types with an IsActive property
            var clrType = entityType.ClrType;
            var isActiveProp = clrType.GetProperty("IsActive");
            if (isActiveProp == null || isActiveProp.PropertyType != typeof(bool))
                continue;

            // Build expression: (e) => EF.Property<bool>(e, "IsActive") == true
            var parameter = Expression.Parameter(clrType, "e");
            var propertyMethod = typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(bool));
            var isActiveProperty = Expression.Call(propertyMethod, parameter, Expression.Constant("IsActive"));
            var body = Expression.Equal(isActiveProperty, Expression.Constant(true));
            var lambda = Expression.Lambda(body, parameter);

            modelBuilder.Entity(clrType).HasQueryFilter(lambda);
        }
    }

    private static void ConfigureSendingTicketLines(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<TicketSendingLine>();
        e.ToTable("sending_ticket_lines", "metal_link");
        e.HasKey(x => x.TicketSendingLineId);
        e.Property(x => x.TicketSendingLineId).HasColumnName("sending_ticket_line_id").ValueGeneratedOnAdd();
        e.Property(x => x.TicketSendingId).HasColumnName("sending_ticket_id").IsRequired();
        e.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        e.Property(x => x.FirstWeightKg).HasColumnName("first_weight_kg");
        e.Property(x => x.SecondWeightKg).HasColumnName("second_weight_kg");
        e.Property(x => x.NetWeightKg).HasColumnName("net_weight_kg").IsRequired();
        e.Property(x => x.UnitPricePerKg).HasColumnName("unit_price_per_kg").IsRequired();
        e.Property(x => x.Tare).HasColumnName("tare").HasDefaultValue(0m);
        e.Property(x => x.Notes).HasColumnName("notes");
        e.Property(x => x.CreatedByOperatorId).HasColumnName("created_by_operator_id").IsRequired();
        e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        e.Property(x => x.CreatedTime).HasColumnName("created_time").HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedTime).HasColumnName("updated_time").HasDefaultValueSql("now()");

        e.HasOne(x => x.TicketSending).WithMany(x => x.Lines).HasForeignKey(x => x.TicketSendingId);
        e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
        e.HasOne(x => x.CreatedByOperator).WithMany().HasForeignKey(x => x.CreatedByOperatorId);
    }

}
