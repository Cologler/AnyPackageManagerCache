using AnyPackageManagerCache.Extensions;
using AnyPackageManagerCache.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    internal class NpmJsSyncService : IBackgroundService
    {
        public static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://replicate.npmjs.com/")
        };

        private volatile bool _stoped = true;
        private readonly IServiceProvider _serviceProvider;
        private readonly PackageIndexUpdateService<NpmJs> _updateHostedService;
        private readonly LocalPackagesMemoryIndexes<NpmJs> _localIndexes;

        public NpmJsSyncService(
            PackageIndexUpdateService<NpmJs> updateHostedService, 
            LocalPackagesMemoryIndexes<NpmJs> localIndexes)
        {
            this._updateHostedService = updateHostedService;
            this._localIndexes = localIndexes;
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
            while (!this._stoped)
            {
                await this.UpdateAsync();
            }
        }

        private async Task UpdateAsync()
        {
            var response = await _http.GetAsync($"_changes?feed=continuous&since=now", HttpCompletionOption.ResponseHeadersRead);
            var serializer = new JsonSerializer();
            var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync(), Encoding.UTF8);
            using (var reader = new JsonTextReader(streamReader))
            {
                reader.CloseInput = false;
                reader.SupportMultipleContent = true;
                while (reader.Read())
                {
                    var change = serializer.Deserialize<ChangeItem>(reader);                    
                    if (this._localIndexes.Contains(change.Id))
                    {
                        this._updateHostedService.Add(change.Id);
                    }
                }
            }
        }

        private class ChangeItem
        {
            [JsonProperty("id")]
            public string Id { get; set; }
        }
    }
}
