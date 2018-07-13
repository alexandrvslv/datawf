using DataWF.Common;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Web.Client
{
    public abstract partial class Client<T, K> : ClientBase, ICRUDClient where T : class
    {
        public static string IdProperty;

        public Client()
        {
            if (IdProperty != null)
                Items.Indexes.Add(EmitInvoker.Initialize<T>(IdProperty));
        }

        public SelectableList<T> Items { get; set; } = new SelectableList<T>();

        protected override void CheckItem(object item)
        {
            if (item?.GetType() == typeof(T))
            {
                Items.Add((T)item);
            }
            base.CheckItem(item);
        }

        public virtual T Get(K id)
        {
            return Items.SelectOne(IdProperty, id) ?? GetAsync(id, CancellationToken.None).Result;
        }

        public abstract Task<List<T>> GetAsync(CancellationToken cancellationToken);

        public Task GetAsync() { return GetAsync(CancellationToken.None); }

        public abstract Task<T> PutAsync(T value, CancellationToken cancellationToken);

        public Task PutAsync(object value) { return PutAsync((T)value, CancellationToken.None); }

        public abstract Task<T> PostAsync(T value, CancellationToken cancellationToken);

        public Task PostAsync(object value) { return PostAsync((T)value, CancellationToken.None); }

        public abstract Task<List<T>> FindAsync(string filter, CancellationToken cancellationToken);

        public Task FindAsync(string filter) { return FindAsync(filter, CancellationToken.None); }

        public abstract Task<T> GetAsync(K id, CancellationToken cancellationToken);

        public Task GetAsync(object id) { return GetAsync((K)id, CancellationToken.None); }

        public abstract Task<bool> DeleteAsync(K id, CancellationToken cancellationToken);

        public Task DeleteAsync(object id) { return DeleteAsync((K)id, CancellationToken.None); }
    }
}