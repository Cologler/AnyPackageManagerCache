using AnyPackageManagerCache.Extensions;
using AnyPackageManagerCache.Models.Pypi;
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

namespace AnyPackageManagerCache.Controllers
{
    [Route("pypi/simple/")]
    [ApiController]
    [TypeFilter(typeof(FeatureFilter), Arguments = new[] { nameof(FeaturesService.Pypi) }, IsReusable = true)]
    public class SimpleController : ControllerBase
    {
        private static readonly HttpClient SimpleHttpClient = new HttpClient();
        private readonly IServiceProvider _serviceProvider;
        private readonly PypiDatabaseService _database;
        private readonly ILogger<SimpleController> _logger;

        public SimpleController(IServiceProvider serviceProvider, PypiDatabaseService database, ILogger<SimpleController> logger)
        {
            this._serviceProvider = serviceProvider;
            this._database = database;
            this._logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            HttpResponseMessage response;
            var remoteUrl = "https://pypi.org/simple/";
            try
            {
                response = await SimpleHttpClient.GetAsync(remoteUrl);
            }
            catch (HttpRequestException e)
            {
                this._logger.LogTrace("GET {} -> {}", remoteUrl, e);
                return this.StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
            }
            var stream = await response.Content.ReadAsStreamAsync();
            this.Response.RegisterForDispose(stream);
            return this.File(stream, "text/html");
        }

        [HttpGet("{packageName}")]
        public async Task<ActionResult> Get(string packageName)
        {
            void UpdatePackageInfo(PypiDatabaseService databaseService, string responseContent)
            {
                var pypiPackageInfo = new PypiPackageInfo
                {
                    PackageName = packageName,
                    Updated = DateTime.UtcNow,
                    RawContent = responseContent
                };
                databaseService.GetPackageInfoDbSet().Upsert(pypiPackageInfo);
            }

            var dbset = this._database.GetPackageInfoDbSet();
            var packageInfo = dbset.FindById(packageName);
            var remoteUrl = $"https://pypi.org/simple/{packageName}/";
            string html;

            if (packageInfo == null || (packageInfo.Updated + TimeSpan.FromSeconds(600)) < DateTime.UtcNow)
            {
                HttpResponseMessage response = null;
                try
                {
                    response = await SimpleHttpClient.GetAsync(remoteUrl);
                }
                catch (HttpRequestException e)
                {
                    this._logger.LogTrace("GET {} -> {}", remoteUrl, e.Message);
                }

                if (response != null)
                {
                    html = await response.Content.ReadAsStringAsync();
                    UpdatePackageInfo(this._database, html);
                } 
                else if (packageInfo != null)
                {
                    // use old cache
                    this._logger.LogTrace("Use cache");
                    html = packageInfo.RawContent;
                }
                else
                {
                    return this.StatusCode((int)HttpStatusCode.InternalServerError, $"unable to fetch {remoteUrl}");
                }
            }
            else
            {
                this._logger.LogTrace("Hit cache: {}", packageName);
                html = packageInfo.RawContent;

                this._serviceProvider.GetRequiredService<WorkerService>().AddJob(async (ioc) =>
                {
                    HttpResponseMessage response;
                    try
                    {
                        response = await SimpleHttpClient.GetAsync(remoteUrl);
                    }
                    catch (HttpRequestException) { return; }
                    UpdatePackageInfo(ioc.GetRequiredService<PypiDatabaseService>(), await response.Content.ReadAsStringAsync());
                });
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            foreach (var el in htmlDoc.DocumentNode.SelectNodes("/html/body/a"))
            {
                var href = el.GetAttributeValue("href", null);
                if (href != null)
                {
                    var newUrl = href.Replace(
                        PythonHostedPackagesController.Prefix, 
                        $"{this.Request.Scheme}://{this.Request.Host.Host}:{this.Request.Host.Port}/pypi/pythonhosted-packages/");
                    el.SetAttributeValue("href", newUrl);
                }
            }
            return this.Content(htmlDoc.GetHtmlString(), "text/html");
        }
    }
}
