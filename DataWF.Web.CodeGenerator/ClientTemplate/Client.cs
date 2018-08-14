using DataWF.Common;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NewNameSpace
{
    public abstract partial class Client<T, K> : ClientBase, ICRUDClient<T> where T : class, new() where K : struct
    {
        public Client(Invoker<T, K?> idInvoker, Invoker<T, int?> typeInvoker, int typeId = 0)
        {
            IdInvoker = idInvoker;
            Items.Indexes.Add(IdInvoker);
            TypeInvoker = typeInvoker;
            TypeId = typeId;
        }

        public Invoker<T, K?> IdInvoker { get; }
        public Invoker<T, int?> TypeInvoker { get; }
        public int TypeId { get; }
        public Type ItemType { get { return typeof(T); } }

        public SelectableList<T> Items { get; set; } = new SelectableList<T>();

        public T DeserializeItem(JsonSerializer serializer, JsonTextReader jreader, Dictionary<IInvoker, object> dictionary = null, object id = null)
        {
            dictionary = dictionary ?? new Dictionary<IInvoker, object>();
            var item = (T)null;
            var invoker = (IInvoker)null;
            var typeId = (object)null;
            while (jreader.Read() && jreader.TokenType != JsonToken.EndObject)
            {
                if (jreader.TokenType == JsonToken.PropertyName)
                {
                    invoker = EmitInvoker.Initialize<T>((string)jreader.Value);
                }
                else if (invoker != null)
                {
                    if (jreader.TokenType == JsonToken.StartObject)
                    {
                        dictionary[invoker] = DeserializeObject(serializer, jreader, invoker.DataType);
                    }
                    else if (jreader.TokenType == JsonToken.StartArray)
                    {
                        dictionary[invoker] = DeserializeArray(serializer, jreader, invoker.DataType);
                    }
                    else
                    {
                        dictionary[invoker] = serializer.Deserialize(jreader, invoker.DataType);
                        if (invoker.Name == IdInvoker?.Name)
                        {
                            id = jreader.Value;
                        }
                        else if (invoker.Name == TypeInvoker?.Name)
                        {
                            typeId = jreader.Value;
                            if (typeId != null)
                            {
                                var type = (int)Helper.Parse(typeId, typeof(int));
                                if (type != TypeId)
                                {
                                    var client = Provider.GetClient(typeof(T), type);
                                    item = (T)client.DeserializeItem(serializer, jreader, dictionary, id);
                                    if (Select(IdInvoker.GetValue(item).Value) == null)
                                    {
                                        Items.Add(item);
                                    }
                                    return item;
                                }
                            }
                        }
                    }
                }
            }
            return DeserializeItem(dictionary, id);
        }

        public T DeserializeItem(Dictionary<IInvoker, object> dictionary, object id)
        {
            var add = false;
            var item = (T)null;
            if (id != null)
            {
                item = Select((K)Helper.Parse(id, typeof(K)));
            }

            if (item == null)
            {
                item = new T();
                add = true;
            }
            foreach (var entry in dictionary)
            {
                entry.Key.SetValue(item, entry.Value);
            }
            if (add)
            {
                Items.Add(item);
            }
            return item;
        }

        object ICRUDClient.DeserializeItem(JsonSerializer serializer, JsonTextReader jreader, Dictionary<IInvoker, object> dictionary, object id)
        {
            return DeserializeItem(serializer, jreader, dictionary, id);
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

        public abstract Task<List<T>> GetAsync(CancellationToken cancellationToken);

        public Task GetAsync() => GetAsync(CancellationToken.None);

        public abstract Task<List<T>> FindAsync(string filter, CancellationToken cancellationToken);

        public Task FindAsync(string filter) { return FindAsync(filter, CancellationToken.None); }

        public abstract Task<T> GetAsync(K id, CancellationToken cancellationToken);

        public Task<T> GetAsync(object id, CancellationToken cancellationToken) => GetAsync((K)id, cancellationToken);

        public Task GetAsync(object id) { return GetAsync((K)id, CancellationToken.None); }

        public abstract Task<T> PutAsync(T value, CancellationToken cancellationToken);

        public Task<T> PutAsync(object value, CancellationToken cancellationToken) => PutAsync((T)value, cancellationToken);

        public Task PutAsync(object value) { return PutAsync((T)value, CancellationToken.None); }

        public abstract Task<T> PostAsync(T value, CancellationToken cancellationToken);

        public Task<T> PostAsync(object value, CancellationToken cancellationToken) => PostAsync((T)value, cancellationToken);

        public Task PostAsync(object value) { return PostAsync((T)value, CancellationToken.None); }

        public abstract Task<bool> DeleteAsync(K id, CancellationToken cancellationToken);

        public Task<bool> DeleteAsync(object id, CancellationToken cancellationToken) => DeleteAsync((K)id, cancellationToken);

        public Task DeleteAsync(object id) { return DeleteAsync((K)id, CancellationToken.None); }

        
    }
}