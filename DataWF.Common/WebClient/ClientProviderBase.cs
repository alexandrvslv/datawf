using System;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class ClientProviderBase : IClientProvider
    {
        Dictionary<Type, ICRUDClient> crudClients = new Dictionary<Type, ICRUDClient>();
        private SelectableList<IClient> clients = new SelectableList<IClient>();

        public ClientProviderBase()
        {

        }

        protected void Add(IClient client)
        {
            clients.Add(client);
            if (client is ICRUDClient crudClient)
            {
                crudClients[crudClient.ItemType] = crudClient;
            }
        }
        public AuthorizationInfo Authorization { get; set; }

        public string BaseUrl { get; set; }

        public SelectableList<IClient> Clients => clients;

        IEnumerable<IClient> IClientProvider.Clients => Clients;

        public ICRUDClient<T> GetClient<T>()
        {
            return (ICRUDClient<T>)GetClient(typeof(T));
        }

        public ICRUDClient GetClient(Type type)
        {
            return crudClients.TryGetValue(type, out var crudClient) ? crudClient : null;
        }

        public ICRUDClient GetClient(Type type, int typeId)
        {
            throw new NotImplementedException();
        }
    }
}