using Microsoft.AspNetCore.Mvc;
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
        [HttpGet]
        public IActionResult Get()
        {
            return this.Ok(); // mean this is a apmc server
        }
    }
}
