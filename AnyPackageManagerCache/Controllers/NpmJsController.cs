using AnyPackageManagerCache.Extensions;
using AnyPackageManagerCache.Features;
using AnyPackageManagerCache.Filters;
using AnyPackageManagerCache.Services;
using AnyPackageManagerCache.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Controllers
{
    [Route("npmjs/registry/")]
    [ApiController]
    [TypeFilter(typeof(FeatureFilter), Arguments = new[] { typeof(NpmJs) }, IsReusable = true)]
    public class NpmJsController : ControllerBase
    {
        private static readonly string NpmJsRegistryPrefix = "https://registry.npmjs.org/";
        private readonly HttpClient _npmJsHttpClient;

        private readonly ILogger _logger;
        private readonly LiteDBDatabaseService<NpmJs> _databaseService;
        private readonly IServiceProvider _serviceProvider;
        private readonly MainService<NpmJs> _mainService;

        public NpmJsController(IServiceProvider serviceProvider, NpmJs npmJs,
            MainService<NpmJs> mainService, ILogger<NpmJsController> logger, 
            LiteDBDatabaseService<NpmJs> databaseService)
        {
            this._serviceProvider = serviceProvider;
            this._mainService = mainService;
            this._logger = logger;
            this._databaseService = databaseService;
            this._npmJsHttpClient = npmJs.PackageIndexRequestBuilder.HttpClient;
        }

        internal class PackagePrefixRewriter : JsonRewriter
        {
            private readonly ControllerBase _controller;

            public PackagePrefixRewriter(ControllerBase controller)
            {
                this._controller = controller;
            }

            protected override void RewriteCore(JObject document)
            {
                var versions = document["versions"] as JObject;
                foreach (var version in versions.Values())
                {
                    var dist = version["dist"];

                    // rewrite tarball
                    var tarball = (JProperty) dist["tarball"].Parent;
                    // https://registry.npmjs.org/anyioc/-/anyioc-0.1.0.tgz
                    // https://registry.npmjs.org/@types/jquery/-/jquery-1.10.21-alpha.tgz
                    tarball.Value = tarball.Value.ToObject<string>().Replace(
                        NpmJsRegistryPrefix,
                        $"{this._controller.Request.Scheme}://{this._controller.Request.Host.ToUriComponent()}/npmjs/registry/");
                }
            }
        }

        [HttpGet("{packageName}")]
        public Task<IActionResult> Get(string packageName)
        {
            this._logger.LogInformation("Query package: {}", packageName);

            return this._mainService.GetPackageIndexInfoAsync(this, packageName, new PackagePrefixRewriter(this), this._logger);
        }

        private async Task<IActionResult> InternalGetTarballAsync(string packageId, string remoteUrl, string fileName)
        {
            var id = $"registry.npmjs.org/{remoteUrl}";

            var pkg = this._databaseService.GetPackageInfoDbSet().FindById(packageId);
            if (pkg == null)
            {
                this._logger.LogInformation("Unable to find package info, fallback to use pipe: {}", packageId);
                return await this._mainService.PipeAsync(this, this._npmJsHttpClient, remoteUrl);
            }

            var hashResult = TryReadHashResultFromJson(pkg.BodyContent, fileName);
            if (hashResult == null)
            {
                this._logger.LogInformation("Unable to find hash info, fallback to use pipe: {}", packageId);
                return await this._mainService.PipeAsync(this, this._npmJsHttpClient, remoteUrl);
            }

            return await this._mainService.GetSmallFileAsync(this, this._databaseService.Database, id,
                this._npmJsHttpClient, remoteUrl, hashResult.Value,
                logger: this._logger);
        }

        [HttpGet("{packageName}/-/{fileName}")]
        public Task<IActionResult> GetTarball(string packageName, string fileName)
        {
            this._logger.LogInformation("Download tarball: {}/{}", packageName, fileName);

            var remoteUrl = $"{packageName}/-/{fileName}";
            return this.InternalGetTarballAsync(packageName, remoteUrl, fileName);
        }

        [HttpGet("{scope}/{packageName}/-/{fileName}")]
        public Task<IActionResult> GetScopedTarball(string scope, string packageName, string fileName)
        {
            this._logger.LogInformation("Download tarball: {}/{}/{}", scope, packageName, fileName);

            // WebUtility.UrlEncode encode @scope/packageName to %40types%2Fjquery, but we need @types%2fjquery
            var packageFullName = $"@{scope}%2f{packageName}";

            var remoteUrl = $"{scope}/{packageName}/-/{fileName}";
            return this.InternalGetTarballAsync(packageFullName, remoteUrl, fileName);
        }

        private static HashResult? TryReadHashResultFromJson(string json, string fileName)
        {
            var document = JObject.Parse(json);
            var versions = document["versions"] as JObject;
            foreach (var version in versions.Values())
            {
                var dist = version["dist"];
                if (dist["tarball"].ToObject<string>().EndsWith(fileName))
                {
                    return new HashResult(HashAlgorithmName.SHA1, dist["shasum"].ToObject<string>());
                }
            }

            return null;
        }
    }
}
