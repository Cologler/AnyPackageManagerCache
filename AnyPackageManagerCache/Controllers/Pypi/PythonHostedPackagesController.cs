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

namespace AnyPackageManagerCache.Controllers
{

    [Route("pypi/pythonhosted-packages/")]
    [ApiController]
    [TypeFilter(typeof(FeatureFilter), Arguments = new[] { nameof(FeaturesService.Pypi) }, IsReusable = true)]
    public class PythonHostedPackagesController : ControllerBase
    {
        internal readonly static string Prefix = "https://files.pythonhosted.org/packages/";

        private static readonly HttpClient PackagesHttpClient = new HttpClient();
        private static readonly Regex FragmentRegex = new Regex("^#(sha256)=(.+)$", RegexOptions.IgnoreCase);

        private readonly PypiDatabaseService _database;
        private readonly ILogger<PythonHostedPackagesController> _logger;

        public PythonHostedPackagesController(PypiDatabaseService database, ILogger<PythonHostedPackagesController> logger)
        {
            this._database = database;
            this._logger = logger;
        }

        static PythonHostedPackagesController()
        {
            PackagesHttpClient.BaseAddress = new Uri(Prefix);
        }

        [HttpGet("{*path}")]
        public async Task<ActionResult<byte[]>> Get(string path)
        {
            var id = $"pythonhosted-packages/{path}";
            var file = this._database.Database.FileStorage.FindById(id);

            if (file == null)
            {
                HttpResponseMessage response;
                try
                {
                    response = await PackagesHttpClient.GetAsync(path);
                }
                catch (HttpRequestException e)
                {
                    this._logger.LogTrace("GET {} -> {}", path, e);
                    return this.StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
                }

                var segments = response.RequestMessage.RequestUri.Segments;
                var filename = segments.Last();

                byte[] buffer;
                try
                {
                    buffer = await response.Content.ReadAsByteArrayAsync();
                }
                catch (IOException e)
                {
                    this._logger.LogTrace("GET {} body -> {}", path, e);
                    return this.StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
                }                

                var match = FragmentRegex.Match(response.RequestMessage.RequestUri.Fragment);
                if (match.Success)
                {
                    if (!buffer.Hash(HashAlgorithmName.SHA256).Equals(match.Groups[2].Value, StringComparison.OrdinalIgnoreCase))
                    {
                        return this.StatusCode((int)HttpStatusCode.InternalServerError, "hashes not matches");
                    }
                }

                file = this._database.Database.FileStorage.Upload(id, filename, new MemoryStream(buffer));
            }

            var downloads = file.Metadata.RawValue.GetValueOrDefault("Downloads", 0) + 1;
            if (downloads > 1)
            {
                this._logger.LogTrace("Hit cache: {} ({})", path, downloads);
            }

            file.Metadata["Downloads"] = downloads;
            file.Metadata["LastUsed"] = DateTime.UtcNow;
            this._database.Database.FileStorage.SetMetadata(id, file.Metadata);
            var stream = file.OpenRead();
            this.Response.RegisterForDispose(stream);
            return this.File(stream, "binary/octet-stream");
        }
    }
}
