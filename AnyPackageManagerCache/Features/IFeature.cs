using AnyPackageManagerCache.Utils;
using LiteDB;

namespace AnyPackageManagerCache.Features
{
    public interface IFeature
    {
        string LiteDBConnectString { get; }

        BsonMapper LiteDBMapper { get; }

        IRequestBuilder PackageIndexRequestBuilder { get; }

        bool IsEnable { get; }
    }
}
