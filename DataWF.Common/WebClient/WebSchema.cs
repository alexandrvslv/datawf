#if NETSTANDARD2_0
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DataWF.Common
{
    [InvokerGenerator(Instance = true)]
    public partial class WebSchema : IWebSchema
    {
        private readonly Dictionary<Type, IWebTable> tables = new Dictionary<Type, IWebTable>();
        private readonly Dictionary<Type, Dictionary<int, IWebTable>> typedTables = new Dictionary<Type, Dictionary<int, IWebTable>>();
        private readonly NamedList<IWebClient> clients = new NamedList<IWebClient>();

        private static HttpClient client;
        private string baseUrl;
        private string authorizationToken;
        private HttpMessageHandler httpMessageHandler;

        public WebSchema()
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
                DefaultBufferSize = 64 * 1024,
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
        public string Name { get; set; }

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

        public IModelProvider Provider { get; set; }

        public SelectableList<IWebClient> Clients => clients;

        IEnumerable<IWebClient> IWebSchema.Clients => Clients;

        IEnumerable<IWebTable> IWebSchema.Tables => Clients.OfType<IWebTable>();

        IEnumerable<IModelTable> IModelSchema.Tables => Clients.OfType<IModelTable>();

        public virtual HttpClient GetHttpClient(HttpMessageHandler httpMessageHandler = null)
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

        protected void Add(IWebClient client)
        {
            clients.Add(client);
            if (client is IWebTable crudClient)
            {
                tables[crudClient.ItemType] = crudClient;
            }
        }

        protected void RefreshTypedCache()
        {
            foreach (var crudClient in clients.OfType<IWebTable>())
            {
                if (crudClient.TypeId != 0)
                {
                    var baseClient = GetTable(crudClient.ItemType.BaseType);
                    while (baseClient != null && baseClient.TypeId != 0 && baseClient.ItemType.BaseType != null)
                    {
                        baseClient = GetTable(baseClient.ItemType.BaseType);
                    }
                    if (baseClient != null)
                    {
                        var baseType = baseClient.ItemType;

                        if (!typedTables.TryGetValue(baseType, out var types))
                        {
                            typedTables[baseType] = types = new Dictionary<int, IWebTable>();
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

        public IWebTable<T> GetTable<T>() => (IWebTable<T>)GetTable(typeof(T));

        public IWebClient GetClient(string name) => clients[name];

        public IWebTable GetTable(Type type)
        {
            if (type.IsInterface || type.IsAbstract)
            {
                if (!tables.TryGetValue(type, out var table))
                {
                    tables[type] = table = Clients.OfType<IWebTable>()
                                                .FirstOrDefault(p => p.ItemType.IsInterface(type));
                }
                return table;
            }
            var baseType = type;
            while (baseType != null)
            {
                if (tables.TryGetValue(baseType, out var webTable))
                    return webTable;
                baseType = baseType.BaseType;
            }

            return null;
        }

        public IWebTable GetTable(Type type, int typeId)
        {
            var baseType = type;
            Dictionary<int, IWebTable> types = null;
            while (baseType != null && !typedTables.TryGetValue(baseType, out types))
            {
                baseType = baseType.BaseType;
            }
            return types != null && types.TryGetValue(typeId, out var client) ? client : null;
        }

        IModelTable<T> IModelSchema.GetTable<T>() => GetTable<T>();
        IModelTable IModelSchema.GetTable(string name) => GetClient(name) as IModelTable;
        IModelTable IModelSchema.GetTable(Type type) => GetTable(type);
        IModelTable IModelSchema.GetTable(Type type, int typeId) => GetTable(type, typeId);
    }
}