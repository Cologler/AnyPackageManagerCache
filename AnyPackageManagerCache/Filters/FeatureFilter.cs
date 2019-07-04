using AnyPackageManagerCache.Features;
using AnyPackageManagerCache.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AnyPackageManagerCache.Filters
{
    public class FeatureFilter : IActionFilter
    {
        public FeatureFilter(Type feature)
        {
            this.Feature = feature;
        }

        public Type Feature { get; }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var service = (IFeature) context.HttpContext.RequestServices.GetRequiredService(this.Feature);
            if (!service.IsEnable)
            {
                context.Result = new NotFoundResult();
            }
        }
    }
}
