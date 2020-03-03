using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.Common
{
    public class ClientProviderBase : IClientProvider
    {
        private static readonly Dictionary<Type, ICrudClient> crudClients = new Dictionary<Type, ICrudClient>();
        private static readonly Dictionary<Type, Dictionary<int, ICrudClient>> crudTypedClients = new Dictionary<Type, Dictionary<int, ICrudClient>>();
        private readonly SelectableList<IClient> clients = new SelectableList<IClient>();
        private readonly Lazy<JsonSerializerOptions> serializeSettings;
        public static ICrudClient<T> Get<T>()
        {
            return (ICrudClient<T>)Get(typeof(T));
        }

        public static ICrudClient Get(Type type)
        {
            return crudClients.TryGetValue(type, out var crudClient) ? crudClient : null;
        }

        public ClientProviderBase()
        {
            serializeSettings = new Lazy<JsonSerializerOptions>(() =>
            {
                var options = new JsonSerializerOptions
                {
#if DEBUG
                    WriteIndented = true,
#endif
                    DefaultBufferSize = 82 * 1024,
                    AllowTrailingCommas = true,
                    // Use the default property (As Is).
                    PropertyNamingPolicy = null,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                // Configure a converters.
                options.Converters.Add(new JsonStringEnumConverter());
                options.Converters.Add(new JsonClientConverterFactory(this));
                return options;
            });
        }

        public JsonSerializerOptions JsonSerializerOptions { get { return serializeSettings.Value; } }

        public AuthorizationInfo Authorization { get; set; }

        public string BaseUrl { get; set; }

        public SelectableList<IClient> Clients => clients;

        IEnumerable<IClient> IClientProvider.Clients => Clients;

        protected void Add(IClient client)
        {
            clients.Add(client);
            if (client is ICrudClient crudClient)
            {
                crudClients[crudClient.ItemType] = crudClient;
            }
        }

        protected void RefreshTypedCache()
        {
            foreach (var crudClient in clients.TypeOf<ICrudClient>())
            {
                if (crudClient.TypeId != 0)
                {
                    var baseType = crudClient.ItemType.BaseType;
                    var baseClien = (ICrudClient)null;
                    while (baseType != null && ((baseClien = GetClient(baseType)) == null || baseClien.TypeId != 0))
                    {
                        baseType = baseType.BaseType;
                    }
                    if (baseClien != null)
                    {
                        if (!crudTypedClients.TryGetValue(baseType, out var types))
                        {
                            crudTypedClients[baseType] = types = new Dictionary<int, ICrudClient>();
                        }
                        types[crudClient.TypeId] = crudClient;
                    }
                }
            }
        }

        public ICrudClient<T> GetClient<T>()
        {
            return (ICrudClient<T>)GetClient(typeof(T));
        }

        public ICrudClient GetClient(Type type)
        {
            return crudClients.TryGetValue(type, out var crudClient) ? crudClient : null;
        }

        public ICrudClient GetClient(Type type, int typeId)
        {
            var baseType = type;
            Dictionary<int, ICrudClient> types = null;
            while (baseType != null && !crudTypedClients.TryGetValue(baseType, out types))
            {
                baseType = baseType.BaseType;
            }
            return types != null && types.TryGetValue(typeId, out var client) ? client : null;
        }
    }
}