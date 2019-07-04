using AnyPackageManagerCache.Extensions;
using AnyPackageManagerCache.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Services
{

    public class PackageIndexUpdateService<T> : IPackageIndexUpdateService, IBackgroundService, IDisposable
        where T : IFeature
    {
        private readonly BlockingCollection<string> _packageNames = new BlockingCollection<string>(10000);
        private readonly T _feature;
        private readonly ILogger<PackageIndexUpdateService<T>> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly HttpClient _httpClient;

        public PackageIndexUpdateService(T feature, ILogger<PackageIndexUpdateService<T>> logger, 
            IServiceProvider serviceProvider)
        {
            this._feature = feature;
            this._logger = logger;
            this._serviceProvider = serviceProvider;
            this._httpClient = this._feature.PackageIndexRequestBuilder.HttpClient;
        }

        public void Add(string packageName)
        {
            this._packageNames.Add(packageName);
        }

        public void Dispose()
        {
            this._packageNames.Dispose();
        }

        public void Start() => Task.Run(this.BeginRun);

        public void Stop() => this._packageNames.CompleteAdding();

        private async void BeginRun()
        {
            foreach (var packageName in this._packageNames.GetConsumingEnumerable())
            {
                if (!await this.Update(packageName))
                {
                    this._logger.LogInformation("Fail to update {}, will try it again.", packageName);
                    this._packageNames.Add(packageName);
                }
                else
                {
                    this._logger.LogInformation("Updated {}.", packageName);
                }
            }
        }

        private async Task<bool> Update(string packageName)
        {
            var remoteUrl = this._feature.PackageIndexRequestBuilder.CreateUrl(packageName);
            using (var response = await this._httpClient.GetOrNullAsync(remoteUrl, HttpCompletionOption.ResponseContentRead, this._logger))
            {
                if (response?.IsSuccessStatusCode == true)
                {
                    using (var scope = this._serviceProvider.CreateScope())
                    {
                        scope.ServiceProvider.GetRequiredService<LiteDBDatabaseService<T>>()
                            .UpdatePackageIndex(packageName, await response.Content.ReadAsStringAsync());
                    }
                }
            }
            return true;
        }
    }
}
