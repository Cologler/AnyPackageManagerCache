﻿using System;
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
using Microsoft.Extensions.Caching.Memory;

namespace AnyPackageManagerCache.Controllers.Pypi
{

    [Route("pypi/pythonhosted-packages/")]
    [ApiController]
    [TypeFilter(typeof(FeatureFilter), Arguments = new[] { typeof(Features.Pypi) }, IsReusable = true)]
    public class PythonHostedPackagesController : ControllerBase
    {
        internal readonly static string Prefix = "https://files.pythonhosted.org/packages/";

        private static readonly HttpClient PackagesHttpClient = new HttpClient();

        private readonly FeaturedServices<Features.Pypi> _pypiServices;
        private readonly ILogger<PythonHostedPackagesController> _logger;
        private readonly IMemoryCache _memoryCache;

        public PythonHostedPackagesController(FeaturedServices<Features.Pypi> pypiServices,
            ILogger<PythonHostedPackagesController> logger, IMemoryCache memoryCache)
        {
            this._pypiServices = pypiServices;
            this._logger = logger;
            this._memoryCache = memoryCache;
        }

        static PythonHostedPackagesController()
        {
            PackagesHttpClient.BaseAddress = new Uri(Prefix);
        }

        [HttpGet("{*path}")]
        public async Task<IActionResult> Get(string path)
        {
            var id = $"pythonhosted-packages/{path}";

            if (!this._memoryCache.TryGetValue<HashResult>(path, out var hashResult))
            {
                this._logger.LogInformation("Unable to parse hash from {}, fallback to use pipe.", path);
                return await this._pypiServices.GetMainService().PipeAsync(this, PackagesHttpClient, path);
            }

            return await this._pypiServices.GetMainService().GetSmallFileAsync(this, id,
                PackagesHttpClient, path, hashResult);
        }
    }
}
