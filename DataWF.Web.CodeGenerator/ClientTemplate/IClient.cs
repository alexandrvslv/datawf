using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Web.Client
{
    public interface IBaseItem
    {
        IClient Client { get; set; }
    }

    public interface IClient
    {
        IClientProvider Provider { get; set; }
    }

    public interface ICRUDClient
    {
        IClientProvider Provider { get; set; }

        Task DeleteAsync(object id);
        Task FindAsync(string filter);
        Task GetAsync();
        Task GetAsync(object id);
        Task PostAsync(object value);
        Task PutAsync(object value);
    }

    public interface IClientProvider
    {
        string BaseUrl { get; set; }
        IEnumerable<IClient> Clients { get; }
        AuthorizationInfo Authorization { get; set; }
    }
}