using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IBaseItem
    {
        IBaseClient Client { get; set; }
    }

    public interface IBaseClient
    {
        IBaseProvider Provider { get; set; }

        Task DeleteAsync(object id);
        Task FindAsync(string filter);
        Task GetAsync();
        Task GetAsync(object id);
        Task PostAsync(object value);
        Task PutAsync(object value);
    }

    public interface IBaseProvider
    {
        string BaseUrl { get; set; }

        IEnumerable<IBaseClient> Clients { get; }
    }
}