using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LexisCampusDMS.Infraestructure.Shared
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Register shared external services (Email, FileStorage, DateTime, etc.) here

            return services;
        }
    }
}
