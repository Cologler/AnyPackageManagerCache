using AnyPackageManagerCache.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Services.Analytics
{
    public class HitService
    {
        private readonly Dictionary<IFeature, HitData> _datas;

        public HitService(IEnumerable<IFeature> features)
        {
            this._datas = features.ToDictionary(z => z, z => new HitData());
        }

        public HitData Get(IFeature feature) => this._datas[feature];

        public class HitData
        {
            public Count QueryIndex { get; } = new Count();

            public Count GetFileCache { get; } = new Count();
        } 

        public class Count
        {
            private int _count;

            public int Value => Volatile.Read(ref this._count);

            public void Increment() => Interlocked.Increment(ref this._count);
        }
    }
}
