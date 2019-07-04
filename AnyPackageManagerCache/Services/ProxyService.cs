using AnyPackageManagerCache.Extensions;
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
    public class ProxyService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public ProxyService(IServiceProvider serviceProvider, ILogger<ProxyService> logger)
        {
            this._serviceProvider = serviceProvider;
            this._logger = logger;
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
            }

            file.Metadata["Downloads"] = downloads;
            file.Metadata["LastUsed"] = DateTime.UtcNow;
            database.FileStorage.SetMetadata(fileId, file.Metadata);
            var stream = file.OpenRead();
            controller.Response.RegisterForDispose(stream);
            return controller.File(stream, "binary/octet-stream", file.Filename);
        }

        public async Task<IActionResult> GetPackageInfoAsync(ControllerBase controller,
            ILiteDBDatabaseService databaseService, string packageName,
            HttpClient httpClient, string remoteUrl,
            ITextRewriter rewriter = null, ILogger logger = null)
        {
            logger = logger ?? this._logger;

            var dbset = databaseService.GetPackageInfoDbSet();
            var packageInfo = dbset.FindById(packageName);
            string body;

            if (packageInfo == null || (packageInfo.Updated + TimeSpan.FromSeconds(600)) < DateTime.UtcNow)
            {
                var response = await httpClient.GetOrNullAsync(remoteUrl, HttpCompletionOption.ResponseContentRead, logger);

                if (response?.IsSuccessStatusCode == true)
                {
                    body = await response.Content.ReadAsStringAsync();
                    databaseService.UpdatePackageIndex(packageName, body);
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
                body = packageInfo.BodyContent;

                this._serviceProvider.GetRequiredService<WorkerService>().AddJob(async (provider, workerLogger) =>
                {
                    var response = await httpClient.GetOrNullAsync(remoteUrl, HttpCompletionOption.ResponseContentRead, workerLogger);
                    if (response?.IsSuccessStatusCode == true)
                    {
                        provider.GetRequiredService<LiteDBDatabaseService<Features.Pypi>>()
                            .UpdatePackageIndex(packageName, await response.Content.ReadAsStringAsync());
                    }
                    response?.Dispose();
                });
            }

            body = rewriter?.Rewrite(body) ?? body;
            return controller.Content(body, "text/html");
        }
    }
}
