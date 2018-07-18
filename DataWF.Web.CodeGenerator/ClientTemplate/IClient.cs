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
    }

    public interface IClient
    {
        IClientProvider Provider { get; set; }
    }

    public interface ICRUDClient : IClient
    {
        Type ItemType { get; }
        Task DeleteAsync(object id);
        Task FindAsync(string filter);
        Task GetAsync();
        Task GetAsync(object id);
        Task PostAsync(object value);
        Task PutAsync(object value);
        object DeserializeItem(JsonSerializer serializer, JsonTextReader jreader);
    }

    public interface ICRUDClient<T> : ICRUDClient
    {
        SelectableList<T> Items { get; }
        new T DeserializeItem(JsonSerializer serializer, JsonTextReader jreader);
    }
}