using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Services
{
    internal class PypiCacheCleanupHostedService : IHostedService, IDisposable
    {
        private static readonly TimeSpan MaxUnusedTimeout = TimeSpan.FromDays(180);
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private Timer _timer;

        public PypiCacheCleanupHostedService(ILogger<PypiCacheCleanupHostedService> logger, IServiceProvider serviceProvider)
        {
            this._logger = logger;
            this._serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("Starting, run after 30s.");
            this._timer = new Timer(this.DoWork, null, TimeSpan.FromSeconds(30), TimeSpan.FromHours(12));
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            this._logger.LogInformation("Working.");

            using (var scope = this._serviceProvider.CreateScope())
            {
                var databaseService = scope.ServiceProvider.GetRequiredService<LiteDBDatabaseService<Features.Pypi>>();

                var remove = new List<string>();
                foreach (var item in databaseService.Database.FileStorage.FindAll())
                {
                    if (!item.Metadata.RawValue.TryGetValue("LastUsed", out var lastUsed))
                    {
                        lastUsed = item.UploadDate;
                    }

                    if (!item.Metadata.RawValue.TryGetValue("Downloads", out var downloads))
                    {
                        downloads = 1;
                    }

                    var timeout = TimeSpan.FromDays(10 * downloads.AsInt32);
                    timeout = MaxUnusedTimeout > timeout ? timeout : MaxUnusedTimeout;
                    var expired = lastUsed.AsDateTime + timeout;
                    if (expired < DateTime.UtcNow)
                    {
                        remove.Add(item.Id);
                    }
                }

                foreach (var id in remove)
                {
                    databaseService.Database.FileStorage.Delete(id);
                }
            }

            this._logger.LogInformation("Cleanup finished.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("Stopping.");
            this._timer?.Change(Timeout.Infinite, Timeout.Infinite);
            return Task.CompletedTask;
        }

        public void Dispose() => this._timer?.Dispose();
    }
}
