using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Models
{
    public class RawPackageInfo
    {
        [BsonId]
        public string PackageName { get; set; }

        [BsonField("Updated")]
        public DateTime Updated { get; set; }

        [BsonField("BodyContent")]
        public string BodyContent { get; set; }
    }
}
