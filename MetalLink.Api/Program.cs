using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MetalLink.Application;
using MetalLink.Infrastructure;
using MetalLink.Infrastructure.Persistence;
using MetalLink.Application.Interfaces;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using MetalLink.Api.Versioning;

QuestPDF.Settings.License = LicenseType.Community;

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

    // Apply pending migrations
    await dbContext.Database.MigrateAsync();
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
