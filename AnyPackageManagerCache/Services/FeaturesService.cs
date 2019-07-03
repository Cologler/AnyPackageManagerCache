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
        public FeaturesService(IConfiguration configuration)
        {
            this.Pypi = configuration.GetSection("Features:pypi").Exists();
        }

        public bool Pypi { get; }

        public bool this[string index]
        {
            get
            {
                switch (index)
                {
                    case nameof(this.Pypi):
                        return this.Pypi;

                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
