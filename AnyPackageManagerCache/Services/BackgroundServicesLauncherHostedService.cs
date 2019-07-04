using AnyPackageManagerCache.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Services
{
    public class BackgroundServicesLauncherHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<IBackgroundService> _services = new List<IBackgroundService>();

        public BackgroundServicesLauncherHostedService(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        private IEnumerable<IBackgroundService> GetBackgroundServices()
        {
            var pypi = this._serviceProvider.GetRequiredService<Pypi>();
            if (pypi.IsEnable)
            {
                yield return this._serviceProvider.GetRequiredService<PackageIndexUpdateService<Pypi>>();
            }

            var npmjs = this._serviceProvider.GetRequiredService<NpmJs>();
            if (npmjs.IsEnable)
            {
                yield return this._serviceProvider.GetRequiredService<PackageIndexUpdateService<NpmJs>>();
                yield return this._serviceProvider.GetRequiredService<NpmJsSyncService>();
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var service in this.GetBackgroundServices())
            {
                service.Start();
                this._services.Add(service);
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._services.ForEach(z => z.Stop());
            return Task.CompletedTask;
        }
    }
}
