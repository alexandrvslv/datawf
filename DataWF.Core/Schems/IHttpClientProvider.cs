using System.Net.Http;

namespace DataWF
{
    public interface IHttpClientProvider
    {
        HttpClientHandler GetHandler();
    }
}
