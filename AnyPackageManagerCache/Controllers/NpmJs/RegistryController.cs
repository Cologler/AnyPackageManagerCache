using AnyPackageManagerCache.Extensions;
using AnyPackageManagerCache.Filters;
using AnyPackageManagerCache.Services;
using AnyPackageManagerCache.Utils;
using Microsoft.AspNetCore.Mvc;
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

namespace AnyPackageManagerCache.Controllers.NpmJs
{
    [Route("npmjs/registry/")]
    [ApiController]
    [TypeFilter(typeof(FeatureFilter), Arguments = new[] { nameof(Features.NpmJs) }, IsReusable = true)]
    public class RegistryController : ControllerBase
    {
        private static readonly string NpmJsRegistryPrefix = "https://registry.npmjs.org/";

        private static readonly HttpClient NpmJsHttpClient = new HttpClient
        {
            BaseAddress = new Uri(NpmJsRegistryPrefix)
        };

        private readonly ILogger _logger;
        private readonly LiteDBDatabaseService<Features.NpmJs> _databaseService;
        private readonly ProxyService _proxyService;
        private readonly PackageIndexUpdateService<Features.NpmJs> _updateService;

        public RegistryController(
            ProxyService proxyService, ILogger<RegistryController> logger, 
            LiteDBDatabaseService<Features.NpmJs> databaseService, PackageIndexUpdateService<Features.NpmJs> updateService)
        {
            this._proxyService = proxyService;
            this._logger = logger;
            this._databaseService = databaseService;
            this._updateService = updateService;
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

            var remoteUrl = $"{packageName}";
            return this._proxyService.GetPackageInfoAsync(
                this, this._databaseService, packageName, NpmJsHttpClient, remoteUrl, 
                this._updateService,
                new PackagePrefixRewriter(this), 
                this._logger);
        }

        private async Task<IActionResult> InternalGetTarballAsync(string packageId, string remoteUrl, string fileName)
        {
            var id = $"registry.npmjs.org/{remoteUrl}";

            var pkg = this._databaseService.GetPackageInfoDbSet().FindById(packageId);
            if (pkg == null)
            {
                return await this._proxyService.PipeAsync(this, NpmJsHttpClient, remoteUrl);
            }

            var hashResult = TryReadHashResultFromJson(pkg.BodyContent, fileName);
            if (hashResult == null)
            {
                return await this._proxyService.PipeAsync(this, NpmJsHttpClient, remoteUrl);
            }

            return await this._proxyService.GetSmallFileAsync(this, this._databaseService.Database, id,
                NpmJsHttpClient, remoteUrl, hashResult.Value,
                logger: this._logger);
        }

        [HttpGet("{packageName}/-/{fileName}")]
        public Task<IActionResult> GetTarball(string packageName, string fileName)
        {
            this._logger.LogInformation("Download tarball: {}", fileName);

            var remoteUrl = $"{packageName}/-/{fileName}";
            return this.InternalGetTarballAsync(packageName, remoteUrl, fileName);
        }

        [HttpGet("{scope}/{packageName}/-/{fileName}")]
        public Task<IActionResult> GetScopedTarball(string scope, string packageName, string fileName)
        {
            this._logger.LogInformation("Download tarball: {}", fileName);

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
