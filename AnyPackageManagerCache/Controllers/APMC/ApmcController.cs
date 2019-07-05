using AnyPackageManagerCache.Features;
using AnyPackageManagerCache.Services.Analytics;
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

        [HttpGet("v1/features")]
        public IActionResult GetFeatures()
        {
            var features = this._serviceProvider.GetServices<IFeature>()
                .Where(z => z.IsEnable)
                .Select(z => z.Name)
                .ToList();
            return this.Ok(features);
        }

        [HttpGet("v1/analytics")]
        public IActionResult GetAnalytics()
        {
            var features = this._serviceProvider.GetServices<IFeature>()
                .Where(z => z.IsEnable)
                .ToList();
            var hitService = this._serviceProvider.GetRequiredService<HitAnalyticService>();
            var analytics = features
                .Select(z => new
                {
                    z.Name,
                    HitInfo = new
                    {
                        QueryIndex = new
                        {
                            Hit = hitService.Get(z).QueryIndex.HitCount.Value,
                            Miss = hitService.Get(z).QueryIndex.MissCount.Value
                        },
                        GetFileCache = new
                        {
                            Hit = hitService.Get(z).GetFileCache.HitCount.Value,
                            Miss = hitService.Get(z).GetFileCache.MissCount.Value
                        },
                    }
                })
                .ToList();
            return this.Ok(new {
                analytics
            });
        }
    }
}
