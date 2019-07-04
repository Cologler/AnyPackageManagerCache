using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> GetOrNullAsync(this HttpClient client, string url, ILogger logger)
        {
            try
            {
                return await client.GetAsync(url);
            }
            catch (HttpRequestException e)
            {
                if (e.InnerException is WebException we)
                {
                    var response = (HttpWebResponse) we.Response;
                    logger.LogInformation("GET {} -> {}", we.Response.ResponseUri, response.StatusCode);
                }
                return null;
            }
        }

        public static async Task<HttpResponseMessage> GetOrNullAsync(this HttpClient client, string url, HttpCompletionOption completionOption, ILogger logger)
        {
            try
            {
                return await client.GetAsync(url, completionOption);
            }
            catch (HttpRequestException e)
            {
                if (e.InnerException is WebException we)
                {
                    var response = (HttpWebResponse)we.Response;
                    logger.LogInformation("GET {} -> {}", we.Response.ResponseUri, response.StatusCode);
                }
                return null;
            }
        }
    }
}
