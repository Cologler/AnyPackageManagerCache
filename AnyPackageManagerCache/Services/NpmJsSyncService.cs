using AnyPackageManagerCache.Extensions;
using AnyPackageManagerCache.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Services
{
    internal class NpmJsSyncService : IDisposable, IHostedService
    {
        public static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://replicate.npmjs.com/")
        };

        private readonly NpmJs _npmJs;
        private readonly PackageIndexUpdateService<NpmJs> _updateHostedService;
        private readonly LocalPackagesMemoryIndexes<NpmJs> _localIndexes;
        private readonly ILogger<NpmJsSyncService> _logger;
        private CancellationTokenSource _cancellationTokenSource;

        public NpmJsSyncService(
            NpmJs npmJs,
            PackageIndexUpdateService<NpmJs> updateHostedService, 
            LocalPackagesMemoryIndexes<NpmJs> localIndexes, ILogger<NpmJsSyncService> logger)
        {
            this._npmJs = npmJs;
            this._updateHostedService = updateHostedService;
            this._localIndexes = localIndexes;
            this._logger = logger;
        }

        public void Dispose()
        {
            this._cancellationTokenSource?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (this._npmJs.IsEnable)
            {
                this._logger.LogInformation("NpmJs sync server started.");
                Task.Run(this.BeginRun);
            }
            else
            {
                this._logger.LogInformation("NpmJs sync server ignore since NpmJs is disabled.");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        private async void BeginRun()
        {
            this._cancellationTokenSource = new CancellationTokenSource();

            try
            {
                while (!this._cancellationTokenSource.IsCancellationRequested)
                {
                    await this.UpdateAsync(this._cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException) { }            
        }

        private async Task UpdateAsync(CancellationToken token)
        {
            try
            {
                var serializer = new JsonSerializer();

                using (var response = await _http.GetAsync($"_changes?feed=continuous&since=now", HttpCompletionOption.ResponseHeadersRead))
                using (var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync(), Encoding.UTF8))
                using (var reader = new JsonTextReader(streamReader))
                {
                    reader.CloseInput = false;
                    reader.SupportMultipleContent = true;
                    while (await reader.ReadAsync(token))
                    {
                        token.ThrowIfCancellationRequested();
                        var change = serializer.Deserialize<ChangeItem>(reader);
                        if (this._localIndexes.Contains(change.Id))
                        {
                            this._updateHostedService.Add(change.Id);
                        }
                    }
                }
            }
            catch (IOException)
            {
                // ignore.
            }
        }

        private class ChangeItem
        {
            [JsonProperty("id")]
            public string Id { get; set; }
        }
    }
}
