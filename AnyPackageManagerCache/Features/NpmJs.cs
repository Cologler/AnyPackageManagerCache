using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AnyPackageManagerCache.Utils;
using LiteDB;
using Microsoft.Extensions.Configuration;

namespace AnyPackageManagerCache.Features
{
    public class NpmJs : IFeature
    {
        public static readonly string NpmJsRegistryPrefix = "https://registry.npmjs.org/";

        public string Name => "npmjs";

        public string LiteDBConnectString => "npmjs.litedb";

        public NpmJs(IConfiguration configuration)
        {
            var sectionRoot = configuration.GetSection("Features:npmjs");
            this.IsEnable = sectionRoot.Exists();
        }

        public BsonMapper LiteDBMapper { get; } = new BsonMapper();

        public IRequestBuilder PackageIndexRequestBuilder { get; } = new RequestBuilder();

        private class RequestBuilder : IRequestBuilder
        {
            public HttpClient HttpClient { get; } = new HttpClient
            {
                BaseAddress = new Uri(NpmJsRegistryPrefix)
            };

            public string CreateUrl(string packageName) => packageName;
        }

        public bool IsEnable { get; }
    }
}
