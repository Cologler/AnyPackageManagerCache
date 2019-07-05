using AnyPackageManagerCache.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Services.Analytics
{
    public class HitAnalyticService
    {
        private readonly Dictionary<IFeature, FeatureData> _datas;

        public HitAnalyticService(IEnumerable<IFeature> features)
        {
            this._datas = features.ToDictionary(z => z, z => new FeatureData());
        }

        public FeatureData Get(IFeature feature) => this._datas[feature];

        public class FeatureData
        {
            public HitProperty QueryIndex { get; } = new HitProperty();

            public HitProperty GetFileCache { get; } = new HitProperty();
        }

        public class HitProperty
        {
            public Counter HitCount { get; } = new Counter();

            public Counter MissCount { get; } = new Counter();

            /// <summary>
            /// Increment hit count
            /// </summary>
            public void Hit() => this.HitCount.Increment();

            /// <summary>
            /// Increment miss count
            /// </summary>
            public void Miss() => this.MissCount.Increment();
        }

        public class Counter
        {
            private int _count;

            public int Value => Volatile.Read(ref this._count);

            public void Increment() => Interlocked.Increment(ref this._count);
        }
    }
}
