using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
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
        ClientStatus Status { get; set; }
    }

    public interface ICRUDClient : IClient
    {
        Type ItemType { get; }
        int TypeId { get; }
        void Add(object item);
        Task DeleteAsync(object id);
        Task FindAsync(string filter);
        Task GetAsync();
        Task GetAsync(object id);
        Task PostAsync(object value);
        Task PutAsync(object value);
        object DeserializeItem(JsonSerializer serializer, JsonTextReader jreader, object item = null, Dictionary<PropertySerializationInfo, object> dictionary = null, object id = null);
    }

    public interface ICRUDClient<T> : ICRUDClient
    {
        SelectableList<T> Items { get; }
        Task<T> Get(T item);
        T Get(object id);
        Task<List<T>> FindAsync(string filter, CancellationToken cancellationToken);
        Task<List<T>> GetAsync(CancellationToken cancellationToken);
        Task<T> GetAsync(object id, CancellationToken cancellationToken);
        Task<T> PostAsync(T value, CancellationToken cancellationToken);
        Task<T> PutAsync(T value, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(object id, CancellationToken cancellationToken);
        T DeserializeItem(JsonSerializer serializer, JsonTextReader jreader, T item, Dictionary<PropertySerializationInfo, object> dictionary = null, object id = null);
    }

    public interface IFileClient
    {
        Task<Stream> DownloadFileAsync(object id);
        Task<string> UploadFileAsync(object id, string fileName, Stream file);
    }

    public enum ClientStatus
    {
        None,
        Compleate,
        Get,
        Put,
        Post,
        Delete,
    }
}