using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MetalLink.Application;
using MetalLink.Infrastructure;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Application.Interfaces;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using MediatR;
using MetalLink.Api.Versioning;

QuestPDF.Settings.License = LicenseType.Community;

// Increase inotify and file descriptor limits at startup to prevent "inotify instance or file descriptor limit" errors (may require elevated permissions or appropriate system configurations)
// This typically requires changing system settings and cannot be fully done from within the app itself.

// Recommended: Increase limits by configuring the system outside this app (e.g., /etc/sysctl.conf and /etc/security/limits.conf).

// For demonstration, attempt to increase limits programmatically (Linux-specific, requires app to run with elevated permissions)
try
{
    const string maxUserInstancesPath = "/proc/sys/fs/inotify/max_user_instances";
    const string maxUserWatchesPath = "/proc/sys/fs/inotify/max_user_watches";

    // Check if running as root
    if (Environment.OSVersion.Platform == PlatformID.Unix && Environment.UserName == "root")
    {
        // Increase max_user_instances
        if (System.IO.File.Exists(maxUserInstancesPath))
        {
            System.IO.File.WriteAllText(maxUserInstancesPath, "524288");
        }

        // Increase max_user_watches
        if (System.IO.File.Exists(maxUserWatchesPath))
        {
            System.IO.File.WriteAllText(maxUserWatchesPath, "524288");
        }

        // Also increase user limits for open files
        var limitsConfPath = "/etc/security/limits.conf";
        if (System.IO.File.Exists(limitsConfPath))
        {
            var limitsContent = System.IO.File.ReadAllText(limitsConfPath);
            if (!limitsContent.Contains("* hard nofile 524288"))
            {
                System.IO.File.AppendAllText(limitsConfPath, "\n* hard nofile 524288\n");
                System.IO.File.AppendAllText(limitsConfPath, "* soft nofile 524288\n");
            }
        }
    }
    else
    {
        Console.WriteLine("Warning: Not running as root. Cannot increase inotify limits programmatically.");
        Console.WriteLine("Please increase the limits manually by running 'sudo sysctl -w fs.inotify.max_user_instances=524288' and 'sudo sysctl -w fs.inotify.max_user_watches=524288'");
        Console.WriteLine("Also, increase user open file descriptor limits by editing /etc/security/limits.conf or your systemd service config.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to increase inotify limits: {ex.Message}");
}

// Note: You may still need to increase user open file descriptor limits (ulimit) outside the app.
// For user limits, edit /etc/security/limits.conf and/or systemd service settings.

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args
});

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MetalLink API",
        Version = AppVersion.GetInformationalVersion(),
        Description = $"Build: {AppVersion.GetInformationalVersion()}"
    });

    // 🔐 Tell Swagger we use Bearer tokens
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme.\n\nExample: 'Bearer 12345abcdef'",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // 🔐 Tell Swagger to require the Bearer scheme globally
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new List<string>()
        }
    });
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>(); 
    //RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
});



// Application (MediatR, FluentValidation)
builder.Services.AddApplicationServices();

// Infrastructure (DbContext, Repositories, UoW, Security services)
builder.Services.AddInfrastructureServices(builder.Configuration);

// JWT configuration
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var signingKey = jwtSection["SigningKey"] ?? throw new InvalidOperationException("JwtSettings:SigningKey not configured");
var issuer = jwtSection["Issuer"];
var audience = jwtSection["Audience"];

var keyBytes = Encoding.UTF8.GetBytes(signingKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-App-Version"] = AppVersion.GetInformationalVersion();
    await next();
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MetalLinkDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    await DbSeeder.SeedAsync(dbContext, passwordHasher);
}

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
