using DataWF.Common;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Web.Client
{
    /// <summary>
    /// Base Client Template
    /// Concept from https://github.com/RSuter/NSwag/wiki/SwaggerToCSharpClientGenerator
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public partial class ClientBase : IClient
    {
        private Lazy<JsonSerializerSettings> settings;
        private string baseUrl;

        public ClientBase()
        {
            settings = new Lazy<JsonSerializerSettings>(() =>
            {
                var settings = new JsonSerializerSettings();
                UpdateJsonSerializerSettings(settings);
                return settings;
            });
        }

        public IClientProvider Provider { get; set; }

        public string BaseUrl
        {
            get { return Provider?.BaseUrl ?? baseUrl; }
            set { baseUrl = value; }
        }

        protected JsonSerializerSettings JsonSerializerSettings { get { return settings.Value; } }

        partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings);
        partial void PrepareRequest(HttpClient client, HttpRequestMessage request, StringBuilder urlBuilder);
        partial void ProcessResponse(HttpClient client, HttpResponseMessage response);

        protected virtual async Task<HttpClient> CreateHttpClientAsync(CancellationToken cancellationToken)
        {
            var client = new HttpClient();
            // TODO: Customize HTTP client
            return client;
        }

        protected virtual StringBuilder ParseUrl(string url, params object[] parameters)
        {
            var urlBuilder = new StringBuilder();
            urlBuilder.Append(BaseUrl?.TrimEnd('/') ?? "");
            int i = 0;
            foreach (var item in url.Split('/'))
            {
                if (item.Length == 0)
                    continue;
                urlBuilder.Append('/');
                if (item.StartsWith("{", StringComparison.Ordinal))
                {
                    if (parameters == null || parameters.Length <= i)
                        throw new ArgumentException();
                    urlBuilder.Append(Uri.EscapeDataString(ConvertToString(parameters[i], System.Globalization.CultureInfo.InvariantCulture)));
                    i++;
                }
                else
                {
                    urlBuilder.Append(item);
                }
            }
            return urlBuilder;
        }

        public virtual async Task<R> Request<R>(CancellationToken cancellationToken,
            string httpMethod = "GET",
            string commandUrl = "/api",
            string mediaType = "application/json",
            object value = null,
            params object[] parameters)
        {
            var urlBuilder = ParseUrl(commandUrl, parameters);
            using (var client = await CreateHttpClientAsync(cancellationToken).ConfigureAwait(false))
            {
                using (var request = new HttpRequestMessage())
                {
                    Provider?.Authorization?.FillRequest(request);

                    if (value != null)
                    {
                        var content = new StringContent(JsonConvert.SerializeObject(value, settings.Value), Encoding.UTF8);
                        content.Headers.ContentType = MediaTypeHeaderValue.Parse(mediaType);
                        request.Content = content;
                    }
                    request.Method = new HttpMethod(httpMethod);
                    if (httpMethod == "GET")
                    {
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
                    }
                    PrepareRequest(client, request, urlBuilder);
                    var url = urlBuilder.ToString();
                    request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);

                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                    {
                        ProcessResponse(client, response);

                        var status = response.StatusCode;
                        if (status == System.Net.HttpStatusCode.OK)
                        {
                            return await ParseResponse<R>(response).ConfigureAwait(false);
                        }
                        else if (status != System.Net.HttpStatusCode.NoContent)
                        {
                            var responseData = response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            throw new ClientException("The HTTP status code of the response was not expected (" + (int)response.StatusCode + ").",
                                (int)response.StatusCode,
                                responseData,
                                GetHeaders(response), null);
                        }

                        return default(R);
                    }
                }
            }
        }

        public Dictionary<string, IEnumerable<string>> GetHeaders(HttpResponseMessage response)
        {
            var headers = Enumerable.ToDictionary(response.Headers, h => h.Key, h => h.Value);
            if (response.Content != null && response.Content.Headers != null)
            {
                foreach (var item in response.Content.Headers)
                    headers[item.Key] = item.Value;
            }
            return headers;
        }

        protected async Task<R> ParseResponse<R>(HttpResponseMessage response)
        {
            using (var responseStream = response.Content == null ? null : await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                if (responseStream != null)
                {
                    try
                    {
                        using (var reader = new StreamReader(responseStream))
                        {
                            if (typeof(R) == typeof(string))
                            {
                                return (R)(object)reader.ReadToEnd();
                            }
                            var serializer = new JsonSerializer();
                            using (var jreader = new JsonTextReader(reader))
                            {
                                while (jreader.Read())
                                {
                                    // deserialize only when there's "{" character in the stream
                                    if (jreader.TokenType == JsonToken.StartArray)
                                    {
                                        return DeserializeArray<R>(serializer, jreader);
                                    }
                                    if (jreader.TokenType == JsonToken.StartObject)
                                    {
                                        return DeserializeObject<R>(serializer, jreader);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var responseData = response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        throw new ClientException("Could not deserialize the response body.", (int)response.StatusCode, responseData, GetHeaders(response), ex);
                    }
                }
            }
            return default(R);
        }

        protected virtual R DeserializeArray<R>(JsonSerializer serializer, JsonTextReader jreader)
        {
            var items = (IList)EmitInvoker.CreateObject<R>();
            var itemType = TypeHelper.GetItemType(typeof(R));
            var client = GetClient(itemType);
            while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
            {
                if (jreader.TokenType == JsonToken.StartObject)
                {
                    items.Add(client == null
                        ? DeserializeByType(serializer, jreader, itemType)
                        : client.DeserializeByType(serializer, jreader, itemType));
                }
            }
            return (R)items;
        }

        public virtual object DeserializeByType(JsonSerializer serializer, JsonTextReader jreader, Type type)
        {
            return serializer.Deserialize(jreader, type);
        }

        protected virtual R DeserializeObject<R>(JsonSerializer serializer, JsonTextReader jreader)
        {
            return (R)DeserializeObject(serializer, jreader, typeof(R));
        }

        protected virtual object DeserializeObject(JsonSerializer serializer, JsonTextReader jreader, Type type)
        {
            var client = GetClient(type);
            if (client != null)
            {
                return client.DeserializeByType(serializer, jreader, type);
            }
            else
            {
                return DeserializeByType(serializer, jreader, type);
            }
        }

        protected ClientBase GetClient(Type type)
        {
            return Provider?.Clients.OfType<ICRUDClient>().FirstOrDefault(p => TypeHelper.IsBaseType(p.ItemType, type)) as ClientBase;
        }

        protected string ConvertToString(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value is Enum)
            {
                string name = Enum.GetName(value.GetType(), value);
                if (name != null)
                {
                    var field = System.Reflection.IntrospectionExtensions.GetTypeInfo(value.GetType()).GetDeclaredField(name);
                    if (field != null)
                    {
                        var attribute = System.Reflection.CustomAttributeExtensions.GetCustomAttribute(field, typeof(System.Runtime.Serialization.EnumMemberAttribute))
                            as System.Runtime.Serialization.EnumMemberAttribute;
                        if (attribute != null)
                        {
                            return attribute.Value;
                        }
                    }
                }
            }
            else if (value is byte[])
            {
                return Convert.ToBase64String((byte[])value);
            }
            else if (value.GetType().IsArray)
            {
                var array = Enumerable.OfType<object>((System.Array)value);
                return string.Join(",", Enumerable.Select(array, o => ConvertToString(o, cultureInfo)));
            }

            return Convert.ToString(value, cultureInfo);
        }
    }
}