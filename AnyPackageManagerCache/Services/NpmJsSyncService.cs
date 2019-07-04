using AnyPackageManagerCache.Extensions;
using AnyPackageManagerCache.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Services
{
    internal class NpmJsSyncService : IBackgroundService
    {
        public static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://replicate.npmjs.com/")
        };

        private volatile bool _stoped = true;
        private readonly object _syncRoot = new object();
        private readonly HashSet<string> _trackedIds = new HashSet<string>();
        private readonly IServiceProvider _serviceProvider;
        private readonly PackageIndexUpdateService<NpmJs> _updateHostedService;

        public NpmJsSyncService(IServiceProvider serviceProvider, PackageIndexUpdateService<NpmJs> updateHostedService)
        {
            this._serviceProvider = serviceProvider;
            this._updateHostedService = updateHostedService;
        }

        public void Start()
        {
            this._stoped = false;
            this.BeginRun();
        }

        public void Stop()
        {
            this._stoped = true;
        }

        private async void BeginRun()
        {
            using (var scope = this._serviceProvider.CreateScope())
            {
                var trackedIds = scope.ServiceProvider
                    .GetRequiredService<LiteDBDatabaseService<NpmJs>>()
                    .GetPackageInfoDbSet()
                    .FindAll()
                    .Select(z => z.PackageName)
                    .ToArray();

                lock (this._syncRoot)
                {
                    foreach (var id in trackedIds)
                    {
                        this._trackedIds.Add(id);
                    }
                }
            }

            string since = "now";
            while (!this._stoped)
            {
                since = await this.UpdateAsync(since);
            }
        }

        private async Task<string> UpdateAsync(string since)
        {
            var response = await _http.GetAsync($"_changes?since={since}", HttpCompletionOption.ResponseContentRead);
            var body = await response.Content.ReadAsStringAsync();
            var changes = JsonConvert.DeserializeObject<Changes>(body);
            string[] needSync;
            lock (this._syncRoot)
            {
                needSync = changes.Results.Select(z => z.Id).Where(z => this._trackedIds.Contains(z)).ToArray();
            }
            if (needSync.Length > 0)
            {
                foreach (var item in needSync)
                {
                    this._updateHostedService.Add(item);
                }
            }
            return changes.LastSeq;
        }

        private class Changes
        {
            [JsonProperty("last_seq")]
            public string LastSeq { get; set; }

            [JsonProperty("results")]
            public List<ChangeItem> Results { get; set; }
        }

        private class ChangeItem
        {
            [JsonProperty("id")]
            public string Id { get; set; }
        }
    }
}
