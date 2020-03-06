using System.Net.Http;

namespace DataWF.WebClient.Common
{
    public interface IHttpClientProvider
    {
        HttpClientHandler GetHandler();
    }
}
