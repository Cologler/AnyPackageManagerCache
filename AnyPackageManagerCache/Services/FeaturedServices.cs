using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnyPackageManagerCache.Features;
using Microsoft.Extensions.DependencyInjection;

namespace AnyPackageManagerCache.Services
{
    public static class FeaturedServices
    {
        internal static IServiceCollection AddFeaturedServices(this IServiceCollection services)
        {
            return services
                .AddScoped(typeof(FeaturedServices<>))
                .AddSingleton(typeof(LocalPackagesMemoryIndexes<>))
                .AddScoped(typeof(LiteDBDatabaseService<>))
                .AddScoped(typeof(MainService<>))
                .AddSingleton(typeof(PackageIndexUpdateService<>));
        }
    }

    public class FeaturedServices<T> where T : IFeature
    {
        private readonly IServiceProvider _serviceProvider;

        public T Feature { get; }

        public FeaturedServices(IServiceProvider serviceProvider, T feature)
        {
            this._serviceProvider = serviceProvider;
            this.Feature = feature;
        }

        public MainService<T> GetMainService() 
            => this._serviceProvider.GetService<MainService<T>>();

        public LocalPackagesMemoryIndexes<T> GetLocalPackagesMemoryIndexes() 
            => this._serviceProvider.GetService<LocalPackagesMemoryIndexes<T>>();

        public LiteDBDatabaseService<T> GetLiteDBDatabaseService() 
            => this._serviceProvider.GetService<LiteDBDatabaseService<T>>();

        public PackageIndexUpdateService<T> GetPackageIndexUpdateService() 
            => this._serviceProvider.GetService<PackageIndexUpdateService<T>>();
    }
}
