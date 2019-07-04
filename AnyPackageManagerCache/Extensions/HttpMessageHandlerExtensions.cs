using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AnyPackageManagerCache.Extensions
{
    public static class HttpMessageHandlerExtensions
    {
        public static HttpMessageHandler Retry(this HttpMessageHandler handler, int times) => new RetryHandler(handler, times);

        private class RetryHandler : DelegatingHandler
        {
            private const int MaxRetries = 3;
            private readonly int _times;

            public RetryHandler(HttpMessageHandler innerHandler, int times) 
                : base(innerHandler)
            {
                this._times = times;
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                HttpResponseMessage response = null;

                for (var i = 0; i < this._times; i++)
                {
                    response = await base.SendAsync(request, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        return response;
                    }
                }

                return response;
            }
        }

    }
}
