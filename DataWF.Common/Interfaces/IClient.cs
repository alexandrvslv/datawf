using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        bool IsSynchronized { get; set; }
        Type ItemType { get; }
        int TypeId { get; }
        bool Add(object item);
        bool Remove(object item);
        void AddDownloads(object id, object item);
        bool RemoveDownloads(object id);
        void RemoveById(object id);
        object Select(object id);
        object ParseId(object id);
        IEnumerable<object> GetChanges();
        Task<object> GenerateId();
        Task<bool> DeleteAsync(object id);
        Task<IEnumerable> FindAsync(string filter);
        Task<IEnumerable> GetAsync();
        Task<object> GetAsync(object id);
        Task<object> CopyAsync(object id);
        Task<object> PostAsync(object value);
        Task<object> PutAsync(object value);
        Task<object> MergeAsync(object id, List<string> ids);
        object DeserializeItem(JsonSerializer serializer, JsonTextReader jreader, object item, IList sourceList);
        object NewItem();
    }

    public interface ICRUDClient<T> : ICRUDClient
    {
        SelectableList<T> Items { get; }
        bool Add(T item);
        bool Remove(T item);
        Task<T> Get(T item);
        T Get(object id);
        Task<bool> Delete(T item);
        LoadProgress<T> Load(string filter, IProgressable progressable);
        Task<List<T>> FindAsync(string filter, ProgressToken progressToken);
        Task<List<T>> GetAsync(ProgressToken progressToken);
        Task<T> GetAsync(object id, ProgressToken progressToken);
        Task<T> CopyAsync(object id, ProgressToken progressToken);
        Task<T> PostAsync(T value, ProgressToken progressToken);
        Task<T> PutAsync(T value, ProgressToken progressToken);
        Task<bool> DeleteAsync(object id, ProgressToken progressToken);
        Task<T> MergeAsync(T value, List<string> ids, ProgressToken progressToken);
        T DeserializeItem(JsonSerializer serializer, JsonTextReader jreader, T item, IList sourceList);
    }

    public interface IFileClient
    {
        Task<Stream> DownloadFileAsync(object id, ProgressToken progressToken);
        Task<string> UploadFileAsync(object id, string fileName, Stream file, ProgressToken progressToken);
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