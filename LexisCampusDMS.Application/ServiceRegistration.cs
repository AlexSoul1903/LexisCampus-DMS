using Microsoft.Extensions.DependencyInjection;

namespace LexisCampusDMS.Application
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            // Register Application services, handlers, validators, mappings here

            return services;
        }
    }
}
