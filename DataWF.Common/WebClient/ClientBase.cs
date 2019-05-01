using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Common
{

    /// <summary>
    /// Base Client Template
    /// Concept from https://github.com/RSuter/NSwag/wiki/SwaggerToCSharpClientGenerator
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public partial class ClientBase : IClient
    {
        private const string fileNameUTFToken = "filename*=UTF-8";
        private const string fileNameToken = "filename=";
        private Lazy<JsonSerializerSettings> serializeSettings;
        private string baseUrl;
        private IClientProvider provider;
        private static HttpClient client;

        public ClientBase()
        {
            serializeSettings = new Lazy<JsonSerializerSettings>(() =>
            {
                var settings = new JsonSerializerSettings()
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    ContractResolver = SynchronizedContractResolver.Instance
                };
                settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                return settings;
            });
        }

        public IClientProvider Provider
        {
            get => provider;
            set => Initialize(value);
        }

        protected virtual void Initialize(IClientProvider provider)
        {
            this.provider = provider;
        }

        public ClientStatus Status { get; set; }

        public string BaseUrl
        {
            get { return Provider?.BaseUrl ?? baseUrl; }
            set { baseUrl = value; }
        }

        protected JsonSerializerSettings JsonSerializerSettings { get { return serializeSettings.Value; } }

        partial void ProcessResponse(HttpClient client, HttpResponseMessage response);

        protected virtual HttpClient CreateHttpClient()
        {
            if (client == null)
            {
                client = new HttpClient() { Timeout = TimeSpan.FromHours(1) };
            }
            return client;
        }

        protected virtual HttpRequestMessage CreateRequest(ProgressToken progressToken,
            string httpMethod = "GET",
            string commandUrl = "/api",
            string mediaType = "application/json",
            object value = null,
            params object[] parameters)
        {
            Status =
                httpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) ? ClientStatus.Post :
                httpMethod.Equals("PUT", StringComparison.OrdinalIgnoreCase) ? ClientStatus.Put :
                httpMethod.Equals("DELETE", StringComparison.OrdinalIgnoreCase) ? ClientStatus.Delete :
                ClientStatus.Get;

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(ParseUrl(commandUrl, parameters).ToString(), UriKind.RelativeOrAbsolute),
                Method = new HttpMethod(httpMethod)
            };

            Provider?.Authorization?.FillRequest(request);

            if (value is Stream stream)
            {
                var fileName = parameters.Length > 1 ? (string)parameters[1]
                    : stream is FileStream fileStream ? Path.GetFileName(fileStream.Name)
                    : "somefile.ext";
                var content = new MultipartFormDataContent
                {
                    { new ProgressStreamContent(progressToken, stream, 81920), Path.GetFileNameWithoutExtension(fileName), fileName }//File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                };
                // content.Headers.ContentType = MediaTypeHeaderValue.Parse(mediaType);
                request.Content = content;
            }
            else if (value != null)
            {
                Validation(value);

                var contentText = JsonConvert.SerializeObject(value, JsonSerializerSettings);
                var content = new StringContent(contentText, Encoding.UTF8);
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(mediaType);
                request.Content = content;
            }
            if (httpMethod == "GET")
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
            }
            return request;
        }

        protected virtual void Validation(object value)
        {
            var vc = new ValidationContext(value);
            //var results = new List<ValidationResult>(); 
            Validator.ValidateObject(value, vc, true);
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

        public virtual async Task<R> Request<R>(ProgressToken progressToken,
            string httpMethod = "GET",
            string commandUrl = "/api",
            string mediaType = "application/json",
            object value = null,
            params object[] parameters)
        {
            var client = CreateHttpClient();

            using (var request = CreateRequest(progressToken, httpMethod, commandUrl, mediaType, value, parameters))
            {
                //if (progressToken?.Process != null)
                //{
                //    client.Timeout = TimeSpan.FromMinutes(15);
                //}
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, progressToken.CancellationToken).ConfigureAwait(false))
                {
                    ProcessResponse(client, response);
                    var result = default(R);
                    switch (response.StatusCode)
                    {
                        case System.Net.HttpStatusCode.OK:
                            if (value is ISynchronized synched)
                            {
                                synched.SyncStatus = SynchronizedStatus.Load;
                            }
                            using (var responseStream = response.Content == null ? null : await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            {
                                using (var reader = new StreamReader(responseStream))
                                {
                                    if (mediaType.Equals("application/octet-stream"))
                                    {
                                        var headers = GetHeaders(response);
                                        (string fileName, int fileSize) = GetFileInfo(headers);
                                        var indentifier = request.RequestUri.LocalPath.Replace("/", "") + (parameters?.FirstOrDefault()?.ToString() ?? "");
                                        var filePath = Helper.GetDocumentsFullPath(fileName, indentifier);
                                        var fileStream = (FileStream)null;
                                        try
                                        {
                                            fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                                        }
                                        catch (IOException ioex)
                                        {
                                            if (ioex.HResult == -2147024864)
                                            {
                                                filePath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(fileName) + "~" + Path.GetExtension(fileName));
                                                fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                                            }
                                            else
                                            {
                                                throw ioex;
                                            }
                                        }
                                        var process = new CopyProcess(CopyProcessCategory.Download);
                                        if (progressToken != ProgressToken.None)
                                        {
                                            progressToken.Process = process;
                                        }
                                        await process.StartAsync(fileSize, responseStream, fileStream);
                                        return (R)(object)fileStream;
                                    }
                                    else if (typeof(R) == typeof(string))
                                    {
                                        result = (R)(object)reader.ReadToEnd();
                                    }
                                    else
                                    {
                                        var serializer = JsonSerializer.Create(JsonSerializerSettings);
                                        using (var jreader = new JsonTextReader(reader))
                                        {
                                            while (jreader.Read())
                                            {
                                                switch (jreader.TokenType)
                                                {
                                                    case JsonToken.StartObject:
                                                        result = DeserializeObject<R>(serializer, jreader, value is R rvalue ? rvalue : default(R), null);
                                                        break;
                                                    case JsonToken.StartArray:
                                                        result = (R)DeserializeArray(serializer, jreader, typeof(R), value as IList);
                                                        break;
                                                    default:
                                                        result = serializer.Deserialize<R>(jreader);
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        case System.Net.HttpStatusCode.Unauthorized:
                            if (Provider?.Authorization?.OnUnauthorizedError() ?? false)
                            {
                                return await Request<R>(progressToken, httpMethod, commandUrl, mediaType, value, parameters).ConfigureAwait(false);
                            }
                            else
                            {
                                ErrorStatus("Unauthorized! Try Relogin!", response, response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            }
                            break;
                        case System.Net.HttpStatusCode.Forbidden:
                            ErrorStatus("Access Denied!", response, response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            break;
                        case System.Net.HttpStatusCode.NotFound:
                            ErrorStatus("No Data Found!", response, response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            break;
                        case System.Net.HttpStatusCode.BadRequest:
                            BadRequest(response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false), response);
                            break;
                        case System.Net.HttpStatusCode.NoContent:
                            result = default(R);
                            break;
                        default:
                            UnexpectedStatus(response, response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            break;
                    }
                    Status = ClientStatus.Compleate;
                    return result;
                }
            }
        }

        private static (string, int) GetFileInfo(Dictionary<string, IEnumerable<string>> headers)
        {
            var fileName = headers.TryGetValue("Content-Disposition", out var disposition)
                ? disposition.FirstOrDefault() : "somefile.someextension";
            fileName = System.Net.WebUtility.UrlDecode(fileName);
            var index = fileName.IndexOf(fileNameUTFToken);
            if (index > 0)
                fileName = fileName.Substring(index + fileNameUTFToken.Length).Trim(' ', '\"', '\'');
            var fileSize = 0;
            if (headers.TryGetValue("Content-Length", out var length))
            {
                int.TryParse(length.FirstOrDefault(), out fileSize);
            }
            return (fileName, fileSize);
        }

        public virtual async Task<R> RequestArray<R, I>(ProgressToken progressToken,
            string httpMethod = "GET",
            string commandUrl = "/api",
            string mediaType = "application/json",
            object value = null,
            params object[] parameters) where R : IList<I>
        {
            var client = CreateHttpClient();
            using (var request = CreateRequest(progressToken, httpMethod, commandUrl, mediaType, value, parameters))
            {
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, progressToken.CancellationToken).ConfigureAwait(false))
                {
                    ProcessResponse(client, response);
                    var result = default(R);
                    switch (response.StatusCode)
                    {
                        case System.Net.HttpStatusCode.OK:

                            using (var responseStream = response.Content == null ? null : await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            {
                                using (var reader = new StreamReader(responseStream))
                                {
                                    var serializer = JsonSerializer.Create(JsonSerializerSettings);
                                    using (var jreader = new JsonTextReader(reader))
                                    {
                                        while (jreader.Read() && jreader.TokenType == JsonToken.StartArray)
                                        {
                                            result = DeserializeArray<R, I>(serializer, jreader);
                                        }
                                    }
                                }
                            }
                            break;
                        case System.Net.HttpStatusCode.Unauthorized:
                            if (Provider?.Authorization?.OnUnauthorizedError() ?? false)
                            {
                                return await RequestArray<R, I>(progressToken, httpMethod, commandUrl, mediaType, value, parameters).ConfigureAwait(false);
                            }
                            else
                            {
                                ErrorStatus("Unauthorized! Try Relogin!", response, response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            }
                            break;
                        case System.Net.HttpStatusCode.NotFound:
                            ErrorStatus("No Data Found!", response, response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            break;
                        case System.Net.HttpStatusCode.NoContent:
                            result = default(R);
                            break;
                        default:
                            UnexpectedStatus(response, response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                            break;
                    }
                    Status = ClientStatus.Compleate;
                    return result;
                }
            }
        }

        protected virtual R DeserializeArray<R, I>(JsonSerializer serializer, JsonTextReader jreader) where R : IList<I>
        {
            var client = Provider.GetClient<I>();
            var items = EmitInvoker.CreateObject<R>();
            var defaultValue = default(I);
            var itemType = typeof(I);
            while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
            {
                if (client != null)
                {
                    items.Add(client.DeserializeItem(serializer, jreader, defaultValue, null));
                }
                else
                {
                    items.Add((I)DeserializeValue(serializer, jreader, itemType, defaultValue, null));
                }
            }
            return items;
        }

        protected virtual IList DeserializeArray(JsonSerializer serializer, JsonTextReader jreader, Type type, IList sourceList)
        {
            if (type == null)
            {
                while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
                {}
                return null;
            }
            var itemType = TypeHelper.GetItemType(type);
            var client = Provider.GetClient(itemType);
            var temp = sourceList ?? (IList)EmitInvoker.CreateObject(type);
            if (!(temp is IFilterable))
            {
                temp.Clear();
            }
            while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
            {
                var item = client != null
                    ? client.DeserializeItem(serializer, jreader, null, sourceList)
                    : DeserializeValue(serializer, jreader, itemType, null, sourceList);
                if (item == null)
                {
                    continue;
                }
                temp.Add(item);
            }

            //if (sourceList != null)
            //{
            //    lock (sourceList)
            //    {
            //        for (var i = 0; i < sourceList.Count;)
            //        {
            //            var item = sourceList[i];
            //            if (item is ISynchronized synched && synched.SyncStatus == SynchronizedStatus.New)
            //            {
            //                i++;
            //                continue;
            //            }
            //            if (!temp.Contains(item))
            //            {
            //                sourceList.RemoveAt(i);
            //            }
            //            else
            //            {
            //                i++;
            //            }
            //        }
            //    }
            //    temp.Clear();
            //    return sourceList;
            //}
            return temp;
        }

        public virtual R DeserializeObject<R>(JsonSerializer serializer, JsonTextReader jreader, R item, IList sourceList)
        {
            var client = Provider.GetClient<R>();
            if (client != null)
            {
                return client.DeserializeItem(serializer, jreader, item, sourceList);
            }

            return (R)DeserializeObject(serializer, jreader, typeof(R), item, sourceList);
        }

        public virtual object DeserializeObject(JsonSerializer serializer, JsonTextReader jreader, Type type, object item, IList sourceList)
        {
            if (type == null)
            {
                while (jreader.Read() && jreader.TokenType != JsonToken.EndObject)
                { }
                return null;
            }

            var client = Provider.GetClient(type);
            if (client != null)
            {
                return client.DeserializeItem(serializer, jreader, item, sourceList);
            }

            var typeInfo = Serialization.Instance.GetTypeInfo(type);
            var property = (PropertySerializationInfo)null;
            item = item ?? typeInfo.Constructor.Create();

            if (item is ISynchronized synchronized
                && (synchronized.SyncStatus == SynchronizedStatus.New
                    || synchronized.SyncStatus == SynchronizedStatus.Actual))
            {
                synchronized.SyncStatus = SynchronizedStatus.Load;
            }
            while (jreader.Read() && jreader.TokenType != JsonToken.EndObject)
            {
                if (jreader.TokenType == JsonToken.PropertyName)
                {
                    property = typeInfo.GetProperty((string)jreader.Value);
                }
                else
                {
                    object value = DeserializeValue(serializer, jreader, property?.DataType, property?.Invoker.GetValue(item), null);
                    if (property == null)
                        continue;
                    property.Invoker.SetValue(item, value);
                }
            }
            if (item is ISynchronized synchronizedNew
                && synchronizedNew.SyncStatus == SynchronizedStatus.Load)
            {
                synchronizedNew.SyncStatus = SynchronizedStatus.Actual;
            }
            return item;
        }

        public object DeserializeValue(JsonSerializer serializer, JsonTextReader jreader, Type type, object item, IList sourceList)
        {
            object value;
            if (jreader.TokenType == JsonToken.StartObject)
            {
                value = DeserializeObject(serializer, jreader, type, item, sourceList);
            }
            else if (jreader.TokenType == JsonToken.StartArray)
            {
                value = DeserializeArray(serializer, jreader, type, item as IList);
            }
            else
            {
                value = serializer.Deserialize(jreader, type);
            }
            if (value != null && sourceList != null && !sourceList.Contains(value))
            {
                sourceList.Add(value);
            }
            return value;
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

        protected string ConvertToString(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value is Enum)
            {
                string name = Enum.GetName(value.GetType(), value);
                if (name != null)
                {
                    var field = IntrospectionExtensions.GetTypeInfo(value.GetType()).GetDeclaredField(name);
                    if (field != null)
                    {
                        var attribute = field.GetCustomAttribute<EnumMemberAttribute>();
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

        private void OnResponceDeserializeException(HttpResponseMessage response, Exception ex)
        {
            throw new ClientException("Could not deserialize the response body.",
                (int)response.StatusCode,
                ex.Message,
                GetHeaders(response), ex);
        }

        private void UnexpectedStatus(HttpResponseMessage response, string responseData)
        {
            throw new ClientException($"Unexpected status code :{response.StatusCode}({(int)response.StatusCode}).",
                (int)response.StatusCode,
                responseData,
                GetHeaders(response), null);
        }

        private void ErrorStatus(string message, HttpResponseMessage response, string responseData)
        {
            throw new ClientException($"{message}",
                (int)response.StatusCode,
                responseData,
                GetHeaders(response), null);
        }

        private void BadRequest(string message, HttpResponseMessage response)
        {
            throw new ClientException($"{message}",
                (int)response.StatusCode,
                null,
                GetHeaders(response), null);
        }

    }
}