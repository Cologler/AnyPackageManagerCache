using AnyPackageManagerCache.Utils;
using LiteDB;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Features
{
    public class Pypi : IFeature
    {
        public string Name => "pypi";

        public string LiteDBConnectString => "pypi.litedb";

        public Pypi(IConfiguration configuration)
        {
            var sectionRoot = configuration.GetSection("Features:pypi");
            this.IsEnable = sectionRoot.Exists();
        }

        public bool IsEnable { get; }

        public BsonMapper LiteDBMapper { get; } = new BsonMapper();

        public IRequestBuilder PackageIndexRequestBuilder { get; } = new RequestBuilder();

        private class RequestBuilder : IRequestBuilder
        {
            public HttpClient HttpClient { get; } = new HttpClient
            {
                BaseAddress = new Uri("https://pypi.org/simple/")
            };

            public string CreateUrl(string packageName) => $"{packageName}/";
        }
    }
}
