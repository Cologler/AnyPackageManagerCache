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

namespace AnyPackageManagerCache.Controllers.Pypi
{
    [Route("pypi/simple/")]
    [ApiController]
    [TypeFilter(typeof(FeatureFilter), Arguments = new[] { nameof(Features.Pypi) }, IsReusable = true)]
    public class SimpleController : ControllerBase
    {
        private static readonly HttpClient SimpleHttpClient = new HttpClient
        {
            BaseAddress = new Uri("https://pypi.org/simple/")
        };

        private readonly LiteDBDatabaseService<Features.Pypi> _database;
        private readonly ProxyService _proxyService;
        private readonly PackageIndexUpdateService<Features.Pypi> _updateService;

        public SimpleController(LiteDBDatabaseService<Features.Pypi> database, ProxyService proxyService,
            PackageIndexUpdateService<Features.Pypi> updateService)
        {
            this._database = database;
            this._proxyService = proxyService;
            this._updateService = updateService;
        }

        [HttpGet]
        public Task<IActionResult> Get() => this._proxyService.PipeAsync(this, SimpleHttpClient, "");

        [HttpGet("{packageName}")]
        public Task<IActionResult> Get(string packageName)
        {
            var remoteUrl = $"{packageName}/";
            return this._proxyService.GetPackageInfoAsync(
                this, this._database, packageName, SimpleHttpClient, remoteUrl, 
                this._updateService, new PrefixRewriter(this));
        }

        internal class PrefixRewriter : HtmlRewriter
        {
            private readonly ControllerBase _controller;

            public PrefixRewriter(ControllerBase controller)
            {
                this._controller = controller;
            }

            protected override void RewriteCore(HtmlDocument document)
            {
                foreach (var el in document.DocumentNode.SelectNodes("/html/body/a"))
                {
                    var href = el.GetAttributeValue("href", null);
                    if (href != null)
                    {
                        var newUrl = href.Replace(
                            PythonHostedPackagesController.Prefix,
                            $"{this._controller.Request.Scheme}://{this._controller.Request.Host.ToUriComponent()}/pypi/pythonhosted-packages/");
                        el.SetAttributeValue("href", newUrl);
                    }
                }
            }
        }

        public static async Task TryUpdatePackageIndexAsync(string packageName, string remoteUrl, IServiceProvider provider, ILogger logger)
        {
            var response = await SimpleHttpClient.GetOrNullAsync(remoteUrl, HttpCompletionOption.ResponseContentRead, logger);
            if (response?.IsSuccessStatusCode == true)
            {
                provider.GetRequiredService<LiteDBDatabaseService<Features.Pypi>>()
                    .UpdatePackageIndex(packageName, await response.Content.ReadAsStringAsync());
            }
            response?.Dispose();
        }
    }
}
