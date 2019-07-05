using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Extensions
{
    public static class ServiceCollectionServiceExtensions
    {
        public static IServiceCollection AddSingletonBoth<TService, TImplementation>(this IServiceCollection services) 
            where TService : class
            where TImplementation : class, TService
        {
            return services
                .AddSingleton<TService, TImplementation>()
                .AddSingleton(ioc => ioc.GetServices<TService>().OfType<TImplementation>().Single());
        }
    }
}
