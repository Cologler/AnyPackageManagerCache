using AnyPackageManagerCache.Features;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Services
{

    public class LocalPackagesMemoryIndexes<T> : ILocalPackagesMemoryIndexes where T : IFeature
    {
        private readonly object _syncRoot = new object();
        private ImmutableHashSet<string> _packageNames;

        public LocalPackagesMemoryIndexes(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                this._packageNames = ImmutableHashSet.CreateRange(scope.ServiceProvider
                    .GetRequiredService<LiteDBDatabaseService<T>>()
                    .GetPackageInfoDbSet()
                    .FindAll()
                    .Select(z => z.PackageName)
                    .ToArray());
            }
        }

        public void Add(string packageName)
        {
            if (this._packageNames.Contains(packageName)) return;

            lock (this._syncRoot)
            {
                this._packageNames = this._packageNames.Add(packageName);
            }
        }

        public void Remove(string packageName)
        {
            lock (this._syncRoot) 
            {
                this._packageNames = this._packageNames.Remove(packageName);
            }
        }

        public bool Contains(string packageName) => this._packageNames.Contains(packageName);
    }
}
