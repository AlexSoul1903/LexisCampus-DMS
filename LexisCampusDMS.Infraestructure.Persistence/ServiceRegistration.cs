using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LexisCampusDMS.Infraestructure.Persistence
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddPersistenceInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Register DbContext, Repositories, UnitOfWork, etc. here

            return services;
        }
    }
}
