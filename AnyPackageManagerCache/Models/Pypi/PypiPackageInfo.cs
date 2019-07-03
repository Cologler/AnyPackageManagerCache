using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Models.Pypi
{
    public class PypiPackageInfo
    {
        [BsonId]
        public string PackageName { get; set; }

        public DateTime Updated { get; set; }

        public string RawContent { get; set; }
    }
}
