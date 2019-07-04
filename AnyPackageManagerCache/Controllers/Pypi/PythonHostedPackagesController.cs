using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AnyPackageManagerCache.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using System.IO;
using LiteDB;
using AnyPackageManagerCache.Filters;
using AnyPackageManagerCache.Extensions;
using AnyPackageManagerCache.Features;
using Microsoft.Extensions.DependencyInjection;
using AnyPackageManagerCache.Utils;

namespace AnyPackageManagerCache.Controllers.Pypi
{

    [Route("pypi/pythonhosted-packages/")]
    [ApiController]
    [TypeFilter(typeof(FeatureFilter), Arguments = new[] { nameof(Features.Pypi) }, IsReusable = true)]
    public class PythonHostedPackagesController : ControllerBase
    {
        internal readonly static string Prefix = "https://files.pythonhosted.org/packages/";

        private static readonly HttpClient PackagesHttpClient = new HttpClient();
        private static readonly Regex HashRegex = new Regex(".+#(sha256)=(.+)$", RegexOptions.IgnoreCase);

        private readonly LiteDBDatabaseService<Features.Pypi> _database;
        private readonly ProxyService _proxyService;

        public PythonHostedPackagesController(LiteDBDatabaseService<Features.Pypi> database, ProxyService proxyService)
        {
            this._database = database;
            this._proxyService = proxyService;
        }

        static PythonHostedPackagesController()
        {
            PackagesHttpClient.BaseAddress = new Uri(Prefix);
        }

        [HttpGet("{*path}")]
        public async Task<IActionResult> Get(string path)
        {
            var id = $"pythonhosted-packages/{path}";

            var match = HashRegex.Match(path);
            if (!match.Success)
            {
                return await this._proxyService.PipeAsync(this, PackagesHttpClient, path);
            }

            return await this._proxyService.GetSmallFileAsync(this, this._database.Database, id,
                PackagesHttpClient, path,
                new HashResult(HashAlgorithmName.SHA256, match.Groups[2].Value));
        }
    }
}
