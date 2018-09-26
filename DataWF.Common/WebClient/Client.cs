using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        public virtual T DeserializeItem(JsonSerializer serializer, JsonTextReader jreader, T item, Dictionary<PropertySerializationInfo, object> dictionary = null, object id = null)
        {
            dictionary = dictionary ?? new Dictionary<PropertySerializationInfo, object>();
            var property = (PropertySerializationInfo)null;
            while (jreader.Read() && jreader.TokenType != JsonToken.EndObject)
            {
                if (jreader.TokenType == JsonToken.PropertyName)
                {
                    property = SerializationInfo.GetProperty((string)jreader.Value);
                }
                else if (property != null)
                {
                    if (jreader.TokenType == JsonToken.StartObject)
                    {
                        dictionary[property] = DeserializeObject(serializer, jreader, property.DataType);
                    }
                    else if (jreader.TokenType == JsonToken.StartArray)
                    {
                        dictionary[property] = DeserializeArray(serializer, jreader, property.DataType);
                    }
                    else
                    {
                        var value = dictionary[property] = serializer.Deserialize(jreader, property.DataType);
                        if (property.Name == IdInvoker?.Name)
                        {
                            id = value;
                        }
                        else if (property.Name == TypeInvoker?.Name)
                        {
                            if (value != null)
                            {
                                var typeId = (int)value;

                                if (typeId != TypeId)
                                {
                                    var client = Provider.GetClient(typeof(T), typeId);
                                    return (T)client.DeserializeItem(serializer, jreader, item, dictionary, id);
                                }
                            }
                        }
                    }
                }
            }
            return DeserializeItem(item, dictionary, id);
        }

        public virtual T DeserializeItem(T item, Dictionary<PropertySerializationInfo, object> dictionary, object id)
        {
            var add = item != null && !Items.Contains(item);

            if (id != null && item == null)
            {
                item = Select((K)id);
            }

            if (item == null)
            {
                item = new T();
                add = true;
            }

            foreach (var entry in dictionary)
            {
                entry.Key.Invoker.SetValue(item, entry.Value);
            }

            if (add)
            {
                Items.Add(item);
                if (TypeId != 0)
                {
                    var baseClient = GetBaseClient();
                    baseClient.Add(item);
                }

            }
            if (item is ISynchronized isSynch)
            {
                isSynch.IsSynchronized = true;
            }
            return item;
        }

        public object DeserializeItem(JsonSerializer serializer, JsonTextReader jreader, object item, Dictionary<PropertySerializationInfo, object> dictionary, object id)
        {
            return DeserializeItem(serializer, jreader, item as T, dictionary, id);
        }

        public override R DeserializeObject<R>(JsonSerializer serializer, JsonTextReader jreader, R item)
        {
            if (typeof(R) == typeof(T))
            {
                return (R)(object)DeserializeItem(serializer, jreader, (T)(object)item);
            }
            return base.DeserializeObject(serializer, jreader, item);
        }

        public ICRUDClient GetBaseClient()
        {
            var type = typeof(T).BaseType;
            while (type != typeof(object))
            {
                var client = Provider.GetClient(type);
                if (client != null)
                    return client;
                type = type.BaseType;
            }
            return null;
        }

        public void Add(object item)
        {
            Items.Add((T)item);
        }

        public void Remove(object item)
        {
            Items.Add((T)item);
        }

        public void RemoveById(object id)
        {
            var item = Select(id);
            if (item is T tItem)
            {
                Items.Remove(tItem);
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
            return Select(id) ?? GetAsync(id, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public virtual Task<List<T>> GetAsync(CancellationToken cancellationToken)
        {
            Items.IsSynchronized = true;
            return Task.FromResult<List<T>>(null);
        }

        public Task GetAsync() => GetAsync(CancellationToken.None);

        public virtual Task<List<T>> FindAsync(string filter, CancellationToken cancellationToken) => Task.FromResult<List<T>>(null);

        public Task FindAsync(string filter) => FindAsync(filter, CancellationToken.None);

        public virtual Task<T> GetAsync(K id, CancellationToken cancellationToken) => Task.FromResult(default(T));

        public Task<T> GetAsync(object id, CancellationToken cancellationToken) => GetAsync((K)id, cancellationToken);

        public Task GetAsync(object id) => GetAsync((K)id, CancellationToken.None);

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


    }


}