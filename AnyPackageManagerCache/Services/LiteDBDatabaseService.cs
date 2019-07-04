using AnyPackageManagerCache.Features;
using AnyPackageManagerCache.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnyPackageManagerCache.Services
{

    public class LiteDBDatabaseService<T> : ILiteDBDatabaseService where T: IFeature
    {
        public LiteDatabase Database { get; }

        public LiteDBDatabaseService(T feature)
        {
            this.Database = new LiteDatabase(feature.LiteDBConnectString, feature.LiteDBMapper);
        }

        public void Dispose() => this.Database?.Dispose();

        public LiteCollection<RawPackageInfo> GetPackageInfoDbSet()
        {
            return this.Database.GetCollection<RawPackageInfo>("PackageInfo");
        }

        public void UpdatePackageIndex(string packageName, string rawContent)
        {
            var packageInfo = new RawPackageInfo
            {
                PackageName = packageName,
                Updated = DateTime.UtcNow,
                BodyContent = rawContent
            };
            this.GetPackageInfoDbSet().Upsert(packageInfo);
        }
    }
}
