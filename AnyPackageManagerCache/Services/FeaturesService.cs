using AnyPackageManagerCache.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Services
{
    public partial class FeaturesService
    {
        private readonly Dictionary<string, bool> _enable = new Dictionary<string, bool>();

        public FeaturesService(IConfiguration configuration)
        {
            this._enable.Add(nameof(Pypi), configuration.GetSection("Features:pypi").Exists());
            this._enable.Add(nameof(NpmJs), configuration.GetSection("Features:npmjs").Exists());
        }

        public IReadOnlyDictionary<string, bool> Enable => this._enable;
    }
}
