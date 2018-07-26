using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DataWF.Web.Client
{
    public interface IBaseItem
    {
        object PrimaryId { get; set; }
        IClient Client { get; set; }
    }

    public interface IClientProvider
    {
        string BaseUrl { get; set; }
        IEnumerable<IClient> Clients { get; }
        AuthorizationInfo Authorization { get; set; }
        ICRUDClient<T> GetClient<T>();
        ICRUDClient GetClient(Type type);
        ICRUDClient GetClient(Type type, int typeId);
    }

    public interface IClient
    {
        IClientProvider Provider { get; set; }
    }

    public interface ICRUDClient : IClient
    {
        Type ItemType { get; }
        int TypeId { get; }
        Task DeleteAsync(object id);
        Task FindAsync(string filter);
        Task GetAsync();
        Task GetAsync(object id);
        Task PostAsync(object value);
        Task PutAsync(object value);
        object DeserializeItem(JsonSerializer serializer, JsonTextReader jreader, Dictionary<IInvoker, object> dictionary = null, object id = null);
    }

    public interface ICRUDClient<T> : ICRUDClient
    {
        SelectableList<T> Items { get; }
        T Get(object id);
        Task<List<T>> FindAsync(string filter, CancellationToken cancellationToken);
        Task<List<T>> GetAsync(CancellationToken cancellationToken);
        Task<T> GetAsync(object id, CancellationToken cancellationToken);
        Task<T> PostAsync(T value, CancellationToken cancellationToken);
        Task<T> PutAsync(T value, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(object id, CancellationToken cancellationToken);
        new T DeserializeItem(JsonSerializer serializer, JsonTextReader jreader, Dictionary<IInvoker, object> dictionary = null, object id = null);
    }
}