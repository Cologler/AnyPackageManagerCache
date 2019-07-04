using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;

namespace AnyPackageManagerCache.Features
{
    public class NpmJs : IFeature
    {
        public string LiteDBConnectString => "npmjs.litedb";

        public BsonMapper LiteDBMapper { get; } = new BsonMapper();
    }
}
