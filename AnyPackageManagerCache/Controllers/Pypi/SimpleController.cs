using AnyPackageManagerCache.Extensions;
using AnyPackageManagerCache.Models;
using AnyPackageManagerCache.Services;
using AnyPackageManagerCache.Filters;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AnyPackageManagerCache.Utils;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Linq;

namespace AnyPackageManagerCache.Controllers.Pypi
{
    [Route("pypi/simple/")]
    [ApiController]
    [TypeFilter(typeof(FeatureFilter), Arguments = new[] { typeof(Features.Pypi) }, IsReusable = true)]
    public class SimpleController : ControllerBase
    {
        private static readonly HttpClient SimpleHttpClient = new HttpClient
        {
            BaseAddress = new Uri("https://pypi.org/simple/")
        };
        private readonly IServiceProvider _serviceProvider;
        private readonly MainService<Features.Pypi> _mainService;
        private readonly ILogger<SimpleController> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly LiteDBDatabaseService<Features.Pypi> _database;
        private readonly PackageIndexUpdateService<Features.Pypi> _updateService;

        public SimpleController(IServiceProvider serviceProvider, MainService<Features.Pypi> mainService, 
            ILogger<SimpleController> logger, IMemoryCache memoryCache,
            LiteDBDatabaseService<Features.Pypi> database,
            PackageIndexUpdateService<Features.Pypi> updateService)
        {
            this._serviceProvider = serviceProvider;
            this._mainService = mainService;
            this._logger = logger;
            this._memoryCache = memoryCache;
            this._database = database;
            this._updateService = updateService;
        }

        [HttpGet]
        public Task<IActionResult> Get() => this._mainService.PipeAsync(this, SimpleHttpClient, "");

        [HttpGet("{packageName}")]
        public Task<IActionResult> Get(string packageName)
        {
            this._logger.LogInformation("Query package: {}", packageName);
            return this._mainService.GetPackageIndexInfoAsync(this, packageName, new PrefixRewriter(this), this._logger);
        }

        private class PrefixRewriter : HtmlRewriter
        {
            private static readonly Regex HashRegex = new Regex("^(.+)#(sha256)=(.+)$", RegexOptions.IgnoreCase);

            private readonly SimpleController _controller;

            public PrefixRewriter(SimpleController controller)
            {
                this._controller = controller;
            }

            protected override void RewriteCore(HtmlDocument document)
            {
                var newPrefix = $"{this._controller.Request.Scheme}://{this._controller.Request.Host.ToUriComponent()}/pypi/pythonhosted-packages/";

                foreach (var el in document.DocumentNode?.SelectNodes("/html/body/a") ?? Enumerable.Empty<HtmlNode>())
                {
                    var href = el.GetAttributeValue("href", null);
                    if (href != null)
                    {
                        var relHref = href.Replace(PythonHostedPackagesController.Prefix, "");

                        var match = HashRegex.Match(relHref);
                        if (match.Success)
                        {
                            var key = match.Groups[1].Value;
                            var value = new HashResult(HashAlgorithmName.SHA256, match.Groups[3].Value);
                            this._controller._memoryCache.Set(key, value);

                            el.SetAttributeValue("href", newPrefix + relHref);
                        }
                        else
                        {
                            this._controller._logger.LogWarning("Unable to parse hash value from {}", href);
                        }
                    }
                }
            }
        }
    }
}
