using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using LexisCampusDMS.Application;
using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Application.Options;
using LexisCampusDMS.Infraestructure.Persistence;
using LexisCampusDMS.Infraestructure.Persistence.Contexts;
using LexisCampusDMS.Infraestructure.Persistence.Seed;
using LexisCampusDMS.Infraestructure.Shared;
using LexisCampusDMS.Server.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add Onion Architecture layers to the container.
builder.Services.AddApplicationLayer();
builder.Services.AddPersistenceInfrastructure(builder.Configuration);
builder.Services.AddSharedInfrastructure(builder.Configuration);

// Presentation / Server dependencies
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LexisCampusDMS.Application.Interfaces.ICurrentUserService, LexisCampusDMS.Server.Services.CurrentUserService>();

// In-Memory Cache for fast verification & performance
builder.Services.AddMemoryCache();

// Rate Limiting (mitigation against scraping and brute-force attacks on public endpoints)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("PublicVerificationRateLimit", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString()
            ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? "anonymous_client";

        return RateLimitPartition.GetFixedWindowLimiter(
            clientIp,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30, // 30 requests per minute per IP
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

// JWT Authentication Configuration
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtSection);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions
{
    Secret = "LexisCampusDMS_SuperSecretKey_2026_Minimum256BitsRequired!",
    Issuer = "LexisCampusDMS",
    Audience = "LexisCampusDMS.Clients"
};

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LexisCampus DMS API",
        Version = "v1",
        Description = "API REST empresarial para el Sistema de Gestión Documental Académica (DMS). Provee servicios de carga con cálculo criptográfico SHA-256, rectificación de versiones inmutables, búsqueda avanzada paginada y descarga con soporte inline/attachment.",
        Contact = new OpenApiContact
        {
            Name = "Equipo de Desarrollo LexisCampus DMS",
            Email = "alexmanuel18frias@gmail.com"
        }
    });

    // JWT Bearer documentation for Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticación JWT usando el esquema Bearer. Ingrese 'Bearer' [espacio] y luego su token en el campo de texto.\r\n\r\nEjemplo: \"Bearer eyJhbGciOiJIUzI1NiIsIn...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// HTTP Security Headers Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("X-XSS-Protection", "0");
    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// AuditLogMiddleware: Captures real IP (X-Forwarded-For) and automatically audits document operations
app.UseMiddleware<AuditLogMiddleware>();

// Rate Limiting middleware for public endpoints
app.UseRateLimiter();

app.MapControllers();

// Automatic migration & institutional seed data on startup
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (dbContext.Database.CanConnect())
    {
        await dbContext.Database.MigrateAsync();
        await DatabaseSeeder.SeedInitialDataAsync(dbContext, passwordHasher, logger);
    }
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Could not run database migrations/seeding at startup. Ensure SQL Server is accessible.");
}

app.Run();
