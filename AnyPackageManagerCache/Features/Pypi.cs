using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Features
{
    public class Pypi : IFeature
    {
        public string LiteDBConnectString => "pypi.litedb";

        public BsonMapper LiteDBMapper { get; } = new BsonMapper();
    }
}
