using AnyPackageManagerCache.Models.Pypi;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace AnyPackageManagerCache.Services
{
    public class PypiDatabaseService : System.IDisposable
    {
        public LiteDatabase Database { get; } = new LiteDatabase("pypi.litedb");

        public void Dispose() => this.Database?.Dispose();

        public LiteCollection<PypiPackageInfo> GetPackageInfoDbSet()
        {
            return this.Database.GetCollection<PypiPackageInfo>("PackageInfo");
        }
    }
}
