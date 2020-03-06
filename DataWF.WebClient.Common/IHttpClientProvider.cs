using System.Net.Http;

namespace DataWF.Common
{
    public interface IHttpClientProvider
    {
        HttpClientHandler GetHandler();
    }
}
