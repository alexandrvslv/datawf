﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace DataWF.Common
{

    public interface IWebProvider : IModelProvider
    {
        new IEnumerable<IWebSchema> Schems { get; }

        IWebClient GetClient(string name);
        new IWebTable<T> GetTable<T>();
        new IWebTable GetTable(Type type);
        new IWebTable GetTable(Type type, int typeId);
    }

    public interface IWebSchema : IModelSchema
    {
        IEnumerable<IWebClient> Clients { get; }
        new IEnumerable<IWebTable> Tables { get; }
        string BaseUrl { get; set; }
        string AuthorizationScheme { get; set; }
        string AuthorizationToken { get; set; }
        Func<Task<bool>> UnauthorizedError { get; set; }
#if NETSTANDARD2_0
        Newtonsoft.Json.JsonSerializerSettings JsonSettings { get; }
#else
        System.Text.Json.JsonSerializerOptions JsonSettings { get; }
#endif  
        HttpClient GetHttpClient(HttpMessageHandler httpMessageHandler = null);
        Task<bool> OnUnauthorized();
        IWebClient GetClient(string name);
        new IWebTable<T> GetTable<T>();
        new IWebTable GetTable(Type type);
        new IWebTable GetTable(Type type, int typeId);
    }

    public interface IWebClient : INamed
    {
        IWebSchema Schema { get; set; }

        WebClientStatus Status { get; set; }

        event EventHandler CacheCleared;

        void ClearCache();
    }

    public interface IWebTable : IModelTable, IWebClient
    {
        IWebTableConverter Converter { get; }
        new IWebTableItemList Items { get; }
        bool IsSynchronized { get; set; }

        object AddDownloads(object id, object item);
        bool RemoveDownloads(object id);
        object GetDownloads(object id);
        void SetDownloads(object id, object item);
        void RemoveById(object id);
        object Select(object id);
        object ParseId(object id);
        IEnumerable GetChanges();

        Task<object> Reload(object item, ProgressToken token);
        Task<IEnumerable> Load(string filter, ProgressToken token);
        ILoadProgress LoadCache(string filter, ProgressToken token);
        Task<object> GenerateId(ProgressToken token);
        Task<bool> Delete(object id, ProgressToken token);
        Task<IEnumerable> Search(string filter, ProgressToken token);
        Task<IEnumerable> Get(ProgressToken token);
        Task<object> Get(object id, ProgressToken token);
        Task<object> Copy(object id, ProgressToken token);
        Task<object> Post(object value, ProgressToken token);
        Task<IList> PostPackage(IList value, ProgressToken token);
        Task<object> Put(object value, ProgressToken token);
        Task<object> Merge(object id, List<string> ids, ProgressToken token);
        object NewItem();
        IEnumerable<object> SearchByValue(IInvoker valueInvoker, object value);

        bool ClearLoadCache(string filter);
    }

    public interface IWebTable<T> : IWebTable, IModelTable<T>
    {
        new WebTableItemList<T> Items { get; }
        T Get(object id);
        Task<T> Reload(T item, ProgressToken token);
        new ICollection<T> GetChanges();
        new Task<List<T>> Load(string filter, ProgressToken token);
        new LoadProgress<T> LoadCache(string filter, ProgressToken token);
        new Task<List<T>> Search(string filter, ProgressToken token);
        new Task<List<T>> Get(ProgressToken token);
        new Task<T> Get(object id, ProgressToken token);
        new Task<T> Copy(object id, ProgressToken token);
        Task<T> Post(T value, ProgressToken token);
        Task<List<T>> PostPackage(List<T> value, ProgressToken token);
        Task<T> Put(T value, ProgressToken token);
        new Task<bool> Delete(object id, ProgressToken token);
        Task<bool> Delete(T item, ProgressToken token);
        Task<T> Merge(T value, List<string> ids, ProgressToken token);
    }

    public interface ILoggedWebTable
    {
        Task<IEnumerable> GetLogs(string filter, ProgressToken token);
        Task<IEnumerable> GetItemLogs(object id, ProgressToken token);

        Task<object> UndoLog(long logId, ProgressToken token);
        Task<object> RedoLog(long logId, ProgressToken token);
        Task<bool> RemoveLog(long logId, ProgressToken token);
    }

    public interface ILoggedWebTable<T, L> : ILoggedWebTable
    {
        new Task<List<L>> GetLogs(string filter, ProgressToken token);
        new Task<List<L>> GetItemLogs(object id, ProgressToken token);

        new Task<T> UndoLog(long logId, ProgressToken token);
        new Task<T> RedoLog(long logId, ProgressToken token);
    }

    public interface IFileWebTable
    {
        string GetFilePath(IFileModel model);
        Task<Stream> DownloadFile(object id, ProgressToken token);
        Task<Stream> DownloadFiles(IEnumerable ids, ProgressToken token);
        Task<object> UploadFile(object id, Stream file, ProgressToken token);
        Task<object> UploadFileModel(object model, ProgressToken token);
    }

    public interface ILoggedFileWebTable : IFileWebTable
    {
        Task<Stream> DownloadLogFile(long logId, ProgressToken token);
    }

    public interface IWebTableItemList : IList
    {
        IWebClient Client { get; }
    }

    public enum WebClientStatus
    {
        None,
        Compleate,
        Get,
        Put,
        Post,
        Delete,
    }
}