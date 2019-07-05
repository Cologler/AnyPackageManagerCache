using AnyPackageManagerCache.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Controllers.APMC
{
    [Route("apmc/")]
    [ApiController]
    public class ApmcController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;

        public ApmcController(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return this.Ok(); // mean this is a apmc server
        }

        [HttpGet("v1/features/")]
        public IActionResult GetFeatures()
        {
            var features = this._serviceProvider.GetServices<IFeature>()
                .Where(z => z.IsEnable)
                .Select(z => z.GetType().Name)
                .ToList();
            return this.Ok(features);
        }
    }
}
