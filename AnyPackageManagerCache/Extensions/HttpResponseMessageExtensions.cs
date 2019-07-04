using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Extensions
{
    public static class HttpResponseMessageExtensions
    {
        public static async ValueTask<(int Code, string Message)> GetErrorInfoAsync(this HttpResponseMessage response)
        {
            if (response is null)
            {
                return ((int)HttpStatusCode.InternalServerError, $"Unable to fetch from remote");
            }

            if (response.IsSuccessStatusCode)
            {
                throw new NotImplementedException();
            }

            return ((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }
    }
}
