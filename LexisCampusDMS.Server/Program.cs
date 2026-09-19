using System.Reflection;
using LexisCampusDMS.Application;
using LexisCampusDMS.Infraestructure.Persistence;
using LexisCampusDMS.Infraestructure.Shared;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add Onion Architecture layers to the container.
builder.Services.AddApplicationLayer();
builder.Services.AddPersistenceInfrastructure(builder.Configuration);
builder.Services.AddSharedInfrastructure(builder.Configuration);

// Presentation / Server dependencies
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LexisCampusDMS.Application.Interfaces.ICurrentUserService, LexisCampusDMS.Server.Services.CurrentUserService>();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
