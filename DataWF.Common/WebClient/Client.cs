using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public abstract partial class Client<T, K> : ClientBase, ICRUDClient<T> where T : class, new() where K : struct
    {
        public Client(Invoker<T, K?> idInvoker, Invoker<T, int?> typeInvoker, int typeId = 0)
        {
            IdInvoker = idInvoker;
            Items.Indexes.Add(IdInvoker);
            TypeInvoker = typeInvoker;
            TypeId = typeId;
            SerializationInfo = new TypeSerializationInfo(typeof(T));
        }

        public TypeSerializationInfo SerializationInfo;

        public Invoker<T, K?> IdInvoker { get; }

        public Invoker<T, int?> TypeInvoker { get; }

        public int TypeId { get; }

        public Type ItemType { get { return typeof(T); } }

        public SelectableList<T> Items { get; set; } = new SelectableList<T>();

        public bool IsSynchronized
        {
            get => Items.IsSynchronized;
            set
            {
                Items.IsSynchronized = value;
                Status = value ? ClientStatus.Compleate : ClientStatus.None;
            }
        }

        public virtual T DeserializeItem(JsonSerializer serializer, JsonTextReader jreader, T item, IList sourceList)
        {
            var add = false;
            var index = -1;
            var property = (PropertySerializationInfo)null;
            var id = (object)null;
            while (jreader.Read() && jreader.TokenType != JsonToken.EndObject)
            {
                if (jreader.TokenType == JsonToken.PropertyName)
                {
                    property = SerializationInfo.GetProperty((string)jreader.Value);
                }
                else
                {
                    object value = null;
                    if (jreader.TokenType == JsonToken.StartObject)
                    {
                        var obj = item == null ? null : property?.Invoker.GetValue(item);
                        value = DeserializeObject(serializer, jreader, property?.DataType, obj, null);
                    }
                    else if (jreader.TokenType == JsonToken.StartArray)
                    {
                        var list = item == null ? null : (IList)property?.Invoker.GetValue(item);
                        value = DeserializeArray(serializer, jreader, property?.DataType, list);
                    }
                    else
                    {
                        value = serializer.Deserialize(jreader, property?.DataType);
                    }
                    if (property != null)
                    {
                        if (property.Name == TypeInvoker?.Name && value != null)
                        {
                            var typeId = (int)value;
                            if (typeId != TypeId)
                            {
                                var client = Provider.GetClient(typeof(T), typeId);
                                return (T)client.DeserializeItem(serializer, jreader, item, (IList)sourceList);
                            }
                            continue;
                        }
                        if (property.Name == IdInvoker?.Name)
                        {
                            id = value;
                            if (item == null && id != null)
                            {
                                item = Select((K)id) ?? (T)sourceList?.Cast<IPrimaryKey>().FirstOrDefault(p => p.PrimaryKey?.Equals(id) ?? false);
                            }
                            if (item == null)
                            {
                                item = new T();
                            }

                            IdInvoker.SetValue(item, id);
                            if (!Items.Contains(item))
                            {
                                add = true;
                                index = Items.AddInternal(item);
                            }
                            if (TypeId != 0)
                            {
                                var baseClient = GetBaseClient();
                                baseClient.Add(item);
                            }
                            continue;
                        }
                        if (item == null)
                        {
                            throw new Exception("Wrong Json properties sequence!");
                        }

                        property.Invoker.SetValue(item, value);
                    }
                }
            }

            if (id != null && item is ISynchronized isSynch)
            {
                isSynch.IsSynchronized = true;
            }
            if (add)
            {
                Items.OnListChanged(NotifyCollectionChangedAction.Add, item, index);
            }

            if (sourceList != null && !sourceList.Contains(item))
            {
                sourceList.Add(item);
            }
            return item;
        }

        public object DeserializeItem(JsonSerializer serializer, JsonTextReader jreader, object item, IList sourceList)
        {
            return DeserializeItem(serializer, jreader, item as T, sourceList);
        }

        public override R DeserializeObject<R>(JsonSerializer serializer, JsonTextReader jreader, R item, IList sourceList)
        {
            if (typeof(R) == typeof(T))
            {
                return (R)(object)DeserializeItem(serializer, jreader, (T)(object)item, sourceList);
            }
            return base.DeserializeObject(serializer, jreader, item, sourceList);
        }

        public ICRUDClient GetBaseClient()
        {
            var type = typeof(T).BaseType;
            var result = (ICRUDClient)null;
            while (type != typeof(object))
            {
                var client = Provider.GetClient(type);
                if (client != null)
                    result = client;
                type = type.BaseType;
            }
            return result;
        }

        public object NewItem()
        {
            return new T();
        }

        public void Add(object item)
        {
            var id = IdInvoker.GetValue(item);
            if (id == null || Select(id) == null)
            {
                Items.Add((T)item);
            }
        }

        public void Remove(object item)
        {
            Items.Remove((T)item);
            if (TypeId != 0)
            {
                var baseClient = GetBaseClient();
                baseClient?.Remove(item);
            }
        }

        public void RemoveById(object id)
        {
            var item = Select(id);

            if (item != null)
            {
                if (item.GetType() != ItemType)
                {
                    var client = Provider.GetClient(item.GetType());
                    client?.Remove(item);
                    return;
                }
                Remove(item);
            }

        }

        public object ParseId(object id)
        {
            return Helper.Parse(id, IdInvoker.DataType);
        }

        public object Select(object id)
        {
            return Select((K)id);
        }

        public virtual T Select(K id)
        {
            return Items.SelectOne(IdInvoker.Name, (K?)id);
        }

        public virtual T Get(object id)
        {
            return Get((K)id);
        }

        public async virtual Task<T> Get(T item)
        {
            return await GetAsync(IdInvoker.GetValue(item), CancellationToken.None).ConfigureAwait(false);
        }

        public virtual T Get(K id)
        {
            var item = Select(id);
            if (item == null)
            {
                try { item = GetAsync(id, CancellationToken.None).Result; }
                catch (Exception ex) { Helper.OnException(ex); }
            }
            return item;
        }

        public virtual Task<List<T>> GetAsync(CancellationToken cancellationToken)
        {
            IsSynchronized = true;
            return Task.FromResult<List<T>>(null);
        }

        public Task GetAsync() => GetAsync(CancellationToken.None);

        public virtual Task<List<T>> FindAsync(string filter, CancellationToken cancellationToken) => Task.FromResult<List<T>>(null);

        public Task FindAsync(string filter) => FindAsync(filter, CancellationToken.None);

        public virtual Task<T> GetAsync(K id, CancellationToken cancellationToken) => Task.FromResult(default(T));

        public Task<T> GetAsync(object id, CancellationToken cancellationToken) => GetAsync((K)id, cancellationToken);

        public Task GetAsync(object id) => GetAsync((K)id, CancellationToken.None);

        public virtual Task<T> CopyAsync(K id, CancellationToken cancellationToken) => Task.FromResult(default(T));

        public Task<T> CopyAsync(object id, CancellationToken cancellationToken) => CopyAsync((K)id, cancellationToken);

        public Task CopyAsync(object id) => CopyAsync((K)id, CancellationToken.None);

        public virtual Task<T> PutAsync(T value, CancellationToken cancellationToken) => Task.FromResult(default(T));

        public Task<T> PutAsync(object value, CancellationToken cancellationToken) => PutAsync((T)value, cancellationToken);

        public Task PutAsync(object value) => PutAsync((T)value, CancellationToken.None);

        public virtual Task<T> PostAsync(T value, CancellationToken cancellationToken) => Task.FromResult(default(T));

        public Task<T> PostAsync(object value, CancellationToken cancellationToken) => PostAsync((T)value, cancellationToken);

        public Task PostAsync(object value) => PostAsync((T)value, CancellationToken.None);

        public virtual Task<bool> DeleteAsync(K id, CancellationToken cancellationToken)
        {
            Items.Remove(Select(id));
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(object id, CancellationToken cancellationToken) => DeleteAsync((K)id, cancellationToken);

        public Task DeleteAsync(object id) => DeleteAsync((K)id, CancellationToken.None);

        public virtual Task<object> GenerateIdAsync(CancellationToken cancellationToken) => Task.FromResult<object>(null);

        public Task<object> GenerateId() => GenerateIdAsync(CancellationToken.None);
    }


}