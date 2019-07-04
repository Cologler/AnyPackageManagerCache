using AnyPackageManagerCache.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace AnyPackageManagerCache.Filters
{
    public class FeatureFilter : IActionFilter
    {
        public FeatureFilter(string feature)
        {
            this.Feature = feature;
        }

        public string Feature { get; }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var service = context.HttpContext.RequestServices.GetRequiredService<FeaturesService>();
            if (!service.Enable[this.Feature])
            {
                context.Result = new NotFoundResult();
            }
        }
    }
}
