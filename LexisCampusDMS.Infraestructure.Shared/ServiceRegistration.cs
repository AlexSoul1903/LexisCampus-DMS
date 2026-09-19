using LexisCampusDMS.Application.Interfaces;
using LexisCampusDMS.Infraestructure.Shared.Options;
using LexisCampusDMS.Infraestructure.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;

namespace LexisCampusDMS.Infraestructure.Shared;

public static class ServiceRegistration
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind MinIO Options
        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));

        // Register IMinioClient
        services.AddSingleton<IMinioClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MinioOptions>>().Value;

            var client = new MinioClient()
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey);

            if (options.UseSsl)
            {
                client = client.WithSSL();
            }

            return client.Build();
        });

        // Register Storage and Hash Services
        services.AddScoped<IStorageService, MinioStorageService>();
        services.AddSingleton<IHashService, Sha256HashService>();

        // Register Authentication & Security Services
        services.AddSingleton<IPasswordHasherService, BcryptPasswordHasherService>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}
