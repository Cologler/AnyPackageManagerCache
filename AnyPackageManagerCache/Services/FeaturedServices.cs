using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnyPackageManagerCache.Features;
using Microsoft.Extensions.DependencyInjection;

namespace AnyPackageManagerCache.Services
{
    public interface IFeaturedServices
    {
        IFeature Feature { get; }

        IEnumerable<IBackgroundService> GetBackgroundServices();
    }

    public static class FeaturedServices
    {
        internal static IServiceCollection AddFeaturedServices(this IServiceCollection services)
        {
            return services
                .AddSingleton(typeof(LocalPackagesMemoryIndexes<>))
                .AddScoped(typeof(LiteDBDatabaseService<>))
                .AddScoped(typeof(MainService<>))
                .AddSingleton(typeof(PackageIndexUpdateService<>))
                .AddTransient(typeof(FeaturedServices<>))
                .AddTransient<IEnumerable<IFeaturedServices>>(ioc => new IFeaturedServices[]
                {
                    ioc.GetRequiredService<FeaturedServices<Pypi>>(),
                    ioc.GetRequiredService<FeaturedServices<NpmJs>>(),
                });
        }
    }

    public class FeaturedServices<T> : IFeaturedServices 
        where T : IFeature
    {
        private readonly IServiceProvider _serviceProvider;

        public T Feature { get; }

        IFeature IFeaturedServices.Feature => this.Feature;

        public FeaturedServices(IServiceProvider serviceProvider, T feature)
        {
            this._serviceProvider = serviceProvider;
            this.Feature = feature;
        }

        public MainService<T> GetMainService() 
            => this._serviceProvider.GetRequiredService<MainService<T>>();

        public LocalPackagesMemoryIndexes<T> GetLocalPackagesMemoryIndexes() 
            => this._serviceProvider.GetRequiredService<LocalPackagesMemoryIndexes<T>>();

        public LiteDBDatabaseService<T> GetLiteDBDatabaseService() 
            => this._serviceProvider.GetRequiredService<LiteDBDatabaseService<T>>();

        public PackageIndexUpdateService<T> GetPackageIndexUpdateService() 
            => this._serviceProvider.GetRequiredService<PackageIndexUpdateService<T>>();

        public IEnumerable<IBackgroundService> GetBackgroundServices()
        {
            yield return this._serviceProvider.GetRequiredService<PackageIndexUpdateService<Pypi>>();
        }
    }
}
