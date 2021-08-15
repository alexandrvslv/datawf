using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IClientProvider
    {
        string BaseUrl { get; set; }
        IEnumerable<IClient> Clients { get; }
        string AuthorizationScheme { get; set; }
        string AuthorizationToken { get; set; }
        Func<Task<bool>> UnauthorizedError { get; set; }
#if NETSTANDARD2_0
        Newtonsoft.Json.JsonSerializerSettings JsonSettings { get; }
#else
        System.Text.Json.JsonSerializerOptions JsonSettings { get; }
#endif  
        HttpClient CreateHttpClient(HttpMessageHandler httpMessageHandler = null);
        Task<bool> OnUnauthorized();
        ICrudClient<T> GetClient<T>();
        ICrudClient GetClient(Type type);
        ICrudClient GetClient(Type type, int typeId);
    }

    public interface IClient
    {
        IClientProvider Provider { get; set; }

        ClientStatus Status { get; set; }

        event EventHandler CacheCleared;

        void ClearCache();
    }

    public interface ICrudClient : IClient
    {
        IClientConverter Converter { get; }
        IList Items { get; }
        bool IsSynchronized { get; set; }
        Type ItemType { get; }
        int TypeId { get; }
        bool Add(object item);
        bool Remove(object item);
        object AddDownloads(object id, object item);
        bool RemoveDownloads(object id);
        object GetDownloads(object id);
        void SetDownloads(object id, object item);
        void RemoveById(object id);
        object Select(object id);
        object ParseId(object id);
        IEnumerable GetChanges();
        Task<object> GenerateId();
        Task<bool> DeleteAsync(object id);
        Task<IEnumerable> SearchAsync(string filter, HttpPageSettings list);
        Task<IEnumerable> GetAsync(HttpPageSettings list);
        Task<object> GetAsync(object id);
        Task<object> CopyAsync(object id);
        Task<object> PostAsync(object value);
        Task<IList> PostPackageAsync(IList value);
        Task<object> PutAsync(object value);
        Task<object> MergeAsync(object id, List<string> ids);
        object NewItem();
        IEnumerable<object> SearchByValue(IInvoker valueInvoker, object value);

        bool ClearLoadCache(string filter);
    }

    public interface ICrudClient<T> : ICrudClient
    {
        new ChangeableList<T> Items { get; }
        bool Add(T item);
        bool Remove(T item);
        Task<T> Get(T item);
        T Get(object id);
        new ICollection<T> GetChanges();
        Task<bool> Delete(T item);
        LoadProgress<T> Load(string filter, HttpPageSettings list, HttpJsonSettings settings, ProgressToken progressToken);
        Task<List<T>> SearchAsync(string filter, HttpPageSettings list, HttpJsonSettings settings, ProgressToken progressToken);
        Task<List<T>> GetAsync(HttpPageSettings list, HttpJsonSettings settings, ProgressToken progressToken);
        Task<T> GetAsync(object id, HttpJsonSettings settings, ProgressToken progressToken);
        Task<T> CopyAsync(object id, HttpJsonSettings settings, ProgressToken progressToken);
        Task<T> PostAsync(T value, HttpJsonSettings settings, ProgressToken progressToken);
        Task<List<T>> PostPackageAsync(List<T> value, HttpJsonSettings settings, ProgressToken progressToken);
        Task<T> PutAsync(T value, HttpJsonSettings settings, ProgressToken progressToken);
        Task<bool> DeleteAsync(object id, HttpJsonSettings settings, ProgressToken progressToken);
        Task<T> MergeAsync(T value, List<string> ids, HttpJsonSettings settings, ProgressToken progressToken);

    }

    public interface ILoggedClient
    {
        Task<IEnumerable> GetLogsAsync(string filter, HttpPageSettings list);
        Task<IEnumerable> GetItemLogsAsync(object id, HttpPageSettings list);

        Task<object> UndoLogAsync(long logId);
        Task<object> RedoLogAsync(long logId);
        Task<bool> RemoveLogAsync(long logId);
    }

    public interface ILoggedClient<T, L> : ILoggedClient
    {
        Task<List<L>> GetLogsAsync(string filter, HttpPageSettings list, HttpJsonSettings settings, ProgressToken progressToken);
        Task<List<L>> GetItemLogsAsync(object id, HttpPageSettings list, HttpJsonSettings settings, ProgressToken progressToken);

        Task<T> UndoLogAsync(long logId, HttpJsonSettings settings, ProgressToken progressToken);
        Task<T> RedoLogAsync(long logId, HttpJsonSettings settings, ProgressToken progressToken);
        Task<bool> RemoveLogAsync(long logId, HttpJsonSettings settings, ProgressToken progressToken);
    }

    public interface IFileClient
    {
        string GetFilePath(IFileModel model);
        Task<Stream> DownloadFileAsync(object id, ProgressToken progressToken);
        Task<Stream> DownloadFilesAsync(IEnumerable ids, ProgressToken progressToken);
        Task<object> UploadFileAsync(object id, Stream file, ProgressToken progressToken);
        Task<object> UploadFileModelAsync(object model, ProgressToken progressToken);
    }

    public interface ILoggedFileClient : IFileClient
    {
        Task<Stream> DownloadLogFileAsync(long logId, ProgressToken progressToken);
    }

    public interface IModelView<T> : IModelView
    {
        new T SelectedItem { get; set; }
        new IFilterable<T> Items { get; set; }
        new Query<T> Filter { get; set; }

        Task<IEnumerable<T>> Get(string filter, HttpPageSettings list = null);
    }

    public interface ICrudModelView<T> : IModelView<T>
    {
        bool AccessCreate { get; }
        bool AccessRead { get; }
        bool AccessUpdate { get; }
        bool AccessAdmin { get; }

        Task<IEnumerable<T>> GetInternal(string filter, HttpPageSettings list = null);
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