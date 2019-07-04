using LiteDB;

namespace AnyPackageManagerCache.Features
{
    public interface IFeature
    {
        string LiteDBConnectString { get; }

        BsonMapper LiteDBMapper { get; }
    }
}
