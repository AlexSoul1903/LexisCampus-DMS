using System.Reflection;
using FluentValidation;
using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LexisCampusDMS.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        // FluentValidation validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Application Services
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
