using AnyPackageManagerCache.Extensions;
using AnyPackageManagerCache.Features;
using AnyPackageManagerCache.Services.Analytics;
using AnyPackageManagerCache.Utils;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Services
{
    public class MainService<T>
        where T : IFeature
    {
        protected readonly IServiceProvider _serviceProvider;
        protected readonly ILogger _logger;
        protected readonly HitService _hitService;
        private readonly T _feature;

        public MainService(IServiceProvider serviceProvider, T feature, ILogger<MainService<T>> logger, HitService hitService)
        {
            this._serviceProvider = serviceProvider;
            this._logger = logger;
            this._hitService = hitService;
            this._feature = feature;
        }

        /// <summary>
        /// pipe request from controller to http-client
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="httpClient"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<IActionResult> PipeAsync(ControllerBase controller, HttpClient httpClient, string url)
        {
            this._logger.LogInformation("Pipe remote {}{}", httpClient.BaseAddress, url);

            var response = await httpClient.GetOrNullAsync(url, HttpCompletionOption.ResponseHeadersRead, this._logger);

            if (response?.IsSuccessStatusCode != true)
            {
                var (code, message) = await response.GetErrorInfoAsync();
                return controller.StatusCode(code, message);
            }

            var stream = await response.Content.ReadAsStreamAsync();
            controller.Response.RegisterForDispose(stream);
            return controller.File(stream, response.Content.Headers.ContentType.MediaType);
        }

        public async Task<IActionResult> GetSmallFileAsync(ControllerBase controller,
            LiteDatabase database, string fileId,
            HttpClient httpClient, string url,
            HashResult hashResult, string fileName = null, ILogger logger = null)
        {
            logger = logger ?? this._logger;

            var file = database.FileStorage.FindById(fileId);

            if (file == null)
            {
                var response = await httpClient.GetOrNullAsync(url, logger);
                if (response?.IsSuccessStatusCode != true)
                {
                    var (code, message) = await response.GetErrorInfoAsync();
                    return controller.StatusCode(code, message);
                }

                if (fileName == null)
                {
                    fileName = response.RequestMessage.RequestUri.Segments.Last();
                }

                byte[] buffer;
                try
                {
                    buffer = await response.Content.ReadAsByteArrayAsync();
                }
                catch (IOException e)
                {
                    logger.LogTrace("GET {} body -> {}", url, e);
                    return controller.StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
                }

                if (!hashResult.Equals(buffer))
                {
                    return controller.StatusCode((int)HttpStatusCode.InternalServerError, "hashes not matches");
                }

                file = database.FileStorage.Upload(fileId, fileName, new MemoryStream(buffer));
            }

            var downloads = file.Metadata.RawValue.GetValueOrDefault("Downloads", 0) + 1;
            if (downloads > 1)
            {
                logger.LogInformation("Hit file cache: {} ({})", url, downloads);
                this.HitData.GetFileCache.Increment();
            }

            file.Metadata["Downloads"] = downloads;
            file.Metadata["LastUsed"] = DateTime.UtcNow;
            database.FileStorage.SetMetadata(fileId, file.Metadata);
            var stream = file.OpenRead();
            controller.Response.RegisterForDispose(stream);
            return controller.File(stream, "binary/octet-stream", file.Filename);
        }

        public async Task<IActionResult> GetPackageIndexInfoAsync(ControllerBase controller, string packageName, 
            ITextRewriter rewriter = null, ILogger logger = null)
        {
            logger = logger ?? this._logger;

            var databaseService = this._serviceProvider.GetRequiredService<LiteDBDatabaseService<T>>();

            var dbset = databaseService.GetPackageInfoDbSet();
            var packageInfo = dbset.FindById(packageName);
            string body;

            if (packageInfo == null || (packageInfo.Updated + TimeSpan.FromSeconds(600)) < DateTime.UtcNow)
            {
                var httpClient = this._feature.PackageIndexRequestBuilder.HttpClient;
                var remoteUrl = this._feature.PackageIndexRequestBuilder.CreateUrl(packageName);
                var response = await httpClient.GetOrNullAsync(remoteUrl, HttpCompletionOption.ResponseContentRead, logger);

                if (response?.IsSuccessStatusCode == true)
                {
                    body = await response.Content.ReadAsStringAsync();
                    databaseService.UpdatePackageIndex(packageName, body);
                    this._serviceProvider.GetService<LocalPackagesMemoryIndexes<T>>()?.Add(packageName);
                }
                else if (packageInfo != null)
                {
                    // use old cache
                    logger.LogTrace("Fallback to use expired cache");
                    body = packageInfo.BodyContent;
                }
                else
                {
                    if (response != null)
                    {
                        logger.LogTrace("Fetch {} -> {}", response.RequestMessage.RequestUri, response.StatusCode);
                    }

                    var (code, message) = await response.GetErrorInfoAsync();
                    return controller.StatusCode(code, message);
                }
            }
            else
            {
                logger.LogInformation("Hit index cache: {}", packageName);
                this.HitData.QueryIndex.Increment();
                body = packageInfo.BodyContent;

                this._serviceProvider.GetService<PackageIndexUpdateService<Pypi>>()?.Add(packageName);
            }

            body = rewriter?.Rewrite(body) ?? body;
            return controller.Content(body, "text/html");
        }

        public HitService.HitData HitData => this._hitService.Get(this._feature);
    }
}
