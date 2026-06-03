using CopyDatabase.Core.Factories;
using CopyDatabase.Core.Validation;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

namespace CopyDatabase.Core
{
    public static class ServiceExtensions {

        /// <summary>
        /// Registers the core services.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection RegisterCoreServices(this IServiceCollection services) {
            services.AddSingleton<AbstractValidator<IDatabaseServerCredentials>, DatabaseServerCredentialValidator>();
            services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
            services.AddSingleton<IConnectionStringBuilderFactory, ConnectionStringBuilderFactory>();
            return services;
        }
    }
}
