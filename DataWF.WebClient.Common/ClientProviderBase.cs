#if NETSTANDARD2_0
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class ClientProviderBase : IClientProvider
    {
        private static readonly Dictionary<Type, ICrudClient> crudClients = new Dictionary<Type, ICrudClient>();
        private static readonly Dictionary<Type, Dictionary<int, ICrudClient>> crudTypedClients = new Dictionary<Type, Dictionary<int, ICrudClient>>();
        private readonly SelectableList<IClient> clients = new SelectableList<IClient>();

        private static HttpClient client;
        private string baseUrl;
        private string authorizationToken;
        private HttpMessageHandler httpMessageHandler;

        public static ICrudClient<T> Get<T>()
        {
            return (ICrudClient<T>)Get(typeof(T));
        }

        public static ICrudClient Get(Type type)
        {
            var baseType = type;
            while (baseType != null)
            {
                if (crudClients.TryGetValue(baseType, out var crudClient))
                    return crudClient;
                baseType = baseType.BaseType;
            }
            return null;
        }

        public static ICrudClient Get(Type type, int typeId)
        {
            var baseType = type;
            Dictionary<int, ICrudClient> types = null;
            while (baseType != null && !crudTypedClients.TryGetValue(baseType, out types))
            {
                baseType = baseType.BaseType;
            }
            return types != null && types.TryGetValue(typeId, out var client) ? client : null;
        }

        public ClientProviderBase()
        {
#if NETSTANDARD2_0
            JsonSettings = new JsonSerializerSettings()
            {
#if DEBUG
                Formatting = Formatting.Indented,
#endif
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ContractResolver = new NewtonJsonContractResolver(this)
            };
            //JsonSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
#else
            JsonSettings = new JsonSerializerOptions
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
            //JsonSettings.Converters.Add(new JsonStringEnumConverter());
            JsonSettings.Converters.Add(new TimeSpanConverter());
            JsonSettings.Converters.Add(new SystemJsonConverterFactory(this));
#endif
        }

#if NETSTANDARD2_0
        public JsonSerializerSettings JsonSettings { get; }
#else
        public JsonSerializerOptions JsonSettings { get; }

#endif
        public string AuthorizationScheme { get; set; } = "Bearer";
        public string AuthorizationToken
        {
            get => authorizationToken;
            set
            {
                if (authorizationToken != value)
                {
                    authorizationToken = value;
                    if (client != null)
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthorizationScheme, AuthorizationToken);
                    }
                }
            }
        }

        public Func<Task<bool>> UnauthorizedError { get; set; }
        public Func<bool> LoginOut { get; set; }

        public string BaseUrl
        {
            get => baseUrl;
            set
            {
                if (baseUrl != value)
                {
                    baseUrl = value;
                    if (client != null)
                    {
                        client.Dispose();
                        client = null;
                        //CreateHttpClient(httpMessageHandler);
                        ClearCache();
                    }
                }
            }
        }

        public SelectableList<IClient> Clients => clients;

        IEnumerable<IClient> IClientProvider.Clients => Clients;

        public virtual void ResetLocalization()
        {
            foreach (var client in Clients)
            {
                if (client is ICrudClient crudClient)
                {
                    crudClient.ResetLocalization();
                }
            }
        }

        public virtual HttpClient CreateHttpClient(HttpMessageHandler httpMessageHandler = null)
        {
            this.httpMessageHandler = httpMessageHandler;
            if (client == null)
            {
                client = httpMessageHandler != null ? new HttpClient(httpMessageHandler, false) : new HttpClient();
                client.Timeout = TimeSpan.FromHours(1);

                if (baseUrl != null)
                {
                    client.BaseAddress = new Uri(baseUrl);
                }
                if (AuthorizationToken != null)
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthorizationScheme, AuthorizationToken);
                }
            }
            return client;
        }

        public async Task<bool> OnUnauthorized()
        {
            if (UnauthorizedError != null)
            {
                return await UnauthorizedError();
            }
            return false;
        }

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
                    var baseClient = Get(crudClient.ItemType.BaseType);
                    while (baseClient != null && baseClient.TypeId != 0 && baseClient.ItemType.BaseType != null)
                    {
                        baseClient = Get(baseClient.ItemType.BaseType);
                    }
                    if (baseClient != null)
                    {
                        var baseType = baseClient.ItemType;

                        if (!crudTypedClients.TryGetValue(baseType, out var types))
                        {
                            crudTypedClients[baseType] = types = new Dictionary<int, ICrudClient>();
                        }
                        types[crudClient.TypeId] = crudClient;
                    }
                }
            }
        }

        public virtual void ClearCache()
        {
            foreach (var client in clients)
            {
                client.ClearCache();
            }
        }

        public ICrudClient<T> GetClient<T>() => Get<T>();

        public ICrudClient GetClient(Type type) => Get(type);

        public ICrudClient GetClient(Type type, int typeId) => Get(type, typeId);

        bool IClientProvider.LoginOut()
        {
            if (LoginOut != null)
            {
                return LoginOut();
            }
            return false;
        }
    }
}