using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Core.Domain.Entities;
using LexisCampusDMS.Infraestructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LexisCampusDMS.Infraestructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedInitialDataAsync(
        ApplicationDbContext context, 
        IPasswordHasherService passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await context.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        logger.LogInformation("Seeding initial institutional users into database...");

        var users = new List<User>
        {
            new(
                username: "admin",
                email: "admin@lexiscampus.edu",
                passwordHash: passwordHasher.HashPassword("Admin123@"),
                fullName: "Administrador del Sistema",
                role: "Admin",
                department: "Tecnología y Seguridad"
            ),
            new(
                username: "registro",
                email: "registro@lexiscampus.edu",
                passwordHash: passwordHasher.HashPassword("Registro123@"),
                fullName: "Analista de Registro y Control",
                role: "Registro",
                department: "Admisiones y Registro Académico"
            ),
            new(
                username: "auditor",
                email: "auditor@lexiscampus.edu",
                passwordHash: passwordHasher.HashPassword("Auditor123@"),
                fullName: "Auditor de Cumplimiento",
                role: "Auditor",
                department: "Auditoría y Certificación"
            )
        };

        await context.Users.AddRangeAsync(users, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully seeded {Count} institutional users (admin, registro, auditor).", users.Count);
    }
}
