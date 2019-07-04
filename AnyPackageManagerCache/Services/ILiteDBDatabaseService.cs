using AnyPackageManagerCache.Models;
using LiteDB;
using System;

namespace AnyPackageManagerCache.Services
{
    public interface ILiteDBDatabaseService : IDisposable
    {
        LiteDatabase Database { get; }

        LiteCollection<RawPackageInfo> GetPackageInfoDbSet();

        void UpdatePackageIndex(string packageName, string rawContent);
    }
}
