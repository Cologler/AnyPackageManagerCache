using AnyPackageManagerCache.Features;
using System.Net.Http;

namespace AnyPackageManagerCache.Utils
{
    public interface IRequestBuilder
    {
        HttpClient HttpClient { get; }

        string CreateUrl(string parameter);
    }
}
