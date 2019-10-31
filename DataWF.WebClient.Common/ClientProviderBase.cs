﻿using System;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class ClientProviderBase : IClientProvider
    {
        private static readonly Dictionary<Type, ICRUDClient> crudClients = new Dictionary<Type, ICRUDClient>();
        private static readonly Dictionary<Type, Dictionary<int, ICRUDClient>> crudTypedClients = new Dictionary<Type, Dictionary<int, ICRUDClient>>();
        private readonly SelectableList<IClient> clients = new SelectableList<IClient>();
        public static ICRUDClient<T> Get<T>()
        {
            return (ICRUDClient<T>)Get(typeof(T));
        }

        public static ICRUDClient Get(Type type)
        {
            return crudClients.TryGetValue(type, out var crudClient) ? crudClient : null;
        }

        public ClientProviderBase()
        {

        }

        public AuthorizationInfo Authorization { get; set; }

        public string BaseUrl { get; set; }

        public SelectableList<IClient> Clients => clients;

        IEnumerable<IClient> IClientProvider.Clients => Clients;

        protected void Add(IClient client)
        {
            clients.Add(client);
            if (client is ICRUDClient crudClient)
            {
                crudClients[crudClient.ItemType] = crudClient;
            }
        }

        protected void RefreshTypedCache()
        {
            foreach (var crudClient in clients.TypeOf<ICRUDClient>())
            {
                if (crudClient.TypeId != 0)
                {
                    var baseType = crudClient.ItemType.BaseType;
                    var baseClien = (ICRUDClient)null;
                    while (baseType != null && ((baseClien = GetClient(baseType)) == null || baseClien.TypeId != 0))
                    {
                        baseType = baseType.BaseType;
                    }
                    if (baseClien != null)
                    {
                        if (!crudTypedClients.TryGetValue(baseType, out var types))
                        {
                            crudTypedClients[baseType] = types = new Dictionary<int, ICRUDClient>();
                        }
                        types[crudClient.TypeId] = crudClient;
                    }
                }
            }
        }

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
            var baseType = type;
            Dictionary<int, ICRUDClient> types = null;
            while (baseType != null && !crudTypedClients.TryGetValue(baseType, out types))
            {
                baseType = baseType.BaseType;
            }
            return types != null && types.TryGetValue(typeId, out var client) ? client : null;
        }
    }
}