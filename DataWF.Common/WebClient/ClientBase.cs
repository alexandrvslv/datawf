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

namespace DataWF.Common
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
                var settings = new JsonSerializerSettings()
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    ContractResolver = SynchronizedContractResolver.Instance
                };
                settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                UpdateJsonSerializerSettings(settings);
                return settings;
            });
        }

        public IClientProvider Provider { get; set; }

        public ClientStatus Status { get; set; }

        public string BaseUrl
        {
            get { return Provider?.BaseUrl ?? baseUrl; }
            set { baseUrl = value; }
        }

        protected JsonSerializerSettings JsonSerializerSettings { get { return settings.Value; } }

        partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings);

        partial void ProcessResponse(HttpClient client, HttpResponseMessage response);

        protected virtual HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            // TODO: Customize HTTP client
            return client;
        }

        protected virtual HttpRequestMessage CreateRequest(string httpMethod = "GET",
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

            if (value is Stream stream && parameters.Length > 1)
            {
                var fileName = (string)parameters[1];
                var content = new MultipartFormDataContent();
                // content.Headers.ContentType = MediaTypeHeaderValue.Parse(mediaType);
                content.Add(new StreamContent(stream), Path.GetFileNameWithoutExtension(fileName), fileName);//File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                request.Content = content;
            }
            else if (value != null)
            {
                var contentText = JsonConvert.SerializeObject(value, settings.Value);
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
            using (var client = CreateHttpClient())
            {
                using (var request = CreateRequest(httpMethod, commandUrl, mediaType, value, parameters))
                {
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                    {
                        ProcessResponse(client, response);
                        var result = default(R);
                        switch (response.StatusCode)
                        {
                            case System.Net.HttpStatusCode.OK:
                                try
                                {
                                    using (var responseStream = response.Content == null ? null : await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                                    {
                                        using (var reader = new StreamReader(responseStream))
                                        {
                                            if (mediaType.Equals("application/octet-stream"))
                                            {
                                                var headers = GetHeaders(response);
                                                string fileName = GetFileName(headers);
                                                var filePath = Path.Combine(Path.GetTempPath(), fileName);

                                                var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                                                await responseStream.CopyToAsync(fileStream, 8192, cancellationToken).ConfigureAwait(false);
                                                await fileStream.FlushAsync();
                                                return (R)(object)fileStream;
                                            }
                                            else if (typeof(R) == typeof(string))
                                            {
                                                result = (R)(object)reader.ReadToEnd();
                                            }
                                            else
                                            {
                                                var serializer = new JsonSerializer();
                                                using (var jreader = new JsonTextReader(reader))
                                                {
                                                    while (jreader.Read())
                                                    {
                                                        switch (jreader.TokenType)
                                                        {
                                                            case JsonToken.StartObject:
                                                                result = DeserializeObject<R>(serializer, jreader, value is R rvalue ? rvalue : default(R));
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
                                }
                                catch (Exception ex)
                                {
                                    OnResponceDeserializeException(response, response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false), ex);
                                }
                                break;
                            case System.Net.HttpStatusCode.Unauthorized:
                                if (Provider?.Authorization?.OnUnauthorizedError() ?? false)
                                {
                                    return await Request<R>(cancellationToken, httpMethod, commandUrl, mediaType, value, parameters).ConfigureAwait(false);
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
        }

        private static string GetFileName(Dictionary<string, IEnumerable<string>> headers)
        {
            var fileName = headers.TryGetValue("Content-Disposition", out var disposition)
                ? disposition.FirstOrDefault() : "somefile.someextension";
            fileName = fileName.Replace("attachment; filename=", "");
            var index = fileName.IndexOf(";");
            return fileName.Substring(0, index > -1 ? index : fileName.Length).Trim(' ', '\"');
        }

        public virtual async Task<R> RequestArray<R, I>(CancellationToken cancellationToken,
            string httpMethod = "GET",
            string commandUrl = "/api",
            string mediaType = "application/json",
            object value = null,
            params object[] parameters) where R : IList<I>
        {
            using (var client = CreateHttpClient())
            {
                using (var request = CreateRequest(httpMethod, commandUrl, mediaType, value, parameters))
                {
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                    {
                        ProcessResponse(client, response);
                        var result = default(R);
                        switch (response.StatusCode)
                        {
                            case System.Net.HttpStatusCode.OK:
                                try
                                {
                                    using (var responseStream = response.Content == null ? null : await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                                    {
                                        using (var reader = new StreamReader(responseStream))
                                        {
                                            var serializer = new JsonSerializer();
                                            using (var jreader = new JsonTextReader(reader))
                                            {
                                                while (jreader.Read() && jreader.TokenType == JsonToken.StartArray)
                                                {
                                                    result = DeserializeArray<R, I>(serializer, jreader);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    OnResponceDeserializeException(response, response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false), ex);
                                }
                                break;
                            case System.Net.HttpStatusCode.Unauthorized:
                                if (Provider?.Authorization?.OnUnauthorizedError() ?? false)
                                {
                                    return await RequestArray<R, I>(cancellationToken, httpMethod, commandUrl, mediaType, value, parameters).ConfigureAwait(false);
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
        }

        protected virtual R DeserializeArray<R, I>(JsonSerializer serializer, JsonTextReader jreader) where R : IList<I>
        {
            var client = Provider.GetClient<I>();
            var items = EmitInvoker.CreateObject<R>();
            if (client != null)
            {
                while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
                {
                    items.Add(client.DeserializeItem(serializer, jreader, default(I)));
                }
            }
            else
            {
                while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
                {
                    items.Add(serializer.Deserialize<I>(jreader));

                }
            }

            return items;
        }

        protected virtual IList DeserializeArray(JsonSerializer serializer, JsonTextReader jreader, Type type, IList list)
        {
            var itemType = TypeHelper.GetItemType(type);
            var client = Provider.GetClient(itemType);
            var items = list ?? (IList)EmitInvoker.CreateObject(type);
            items.Clear();
            if (client != null)
            {
                while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
                {
                    items.Add(client.DeserializeItem(serializer, jreader, null));
                }
            }
            else
            {
                while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
                {
                    items.Add(serializer.Deserialize(jreader, itemType));
                }
            }

            return items;
        }

        public virtual R DeserializeObject<R>(JsonSerializer serializer, JsonTextReader jreader, R item)
        {
            var client = Provider.GetClient<R>();
            if (client != null)
            {
                return client.DeserializeItem(serializer, jreader, item);
            }

            return serializer.Deserialize<R>(jreader);
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

        private void OnResponceDeserializeException(HttpResponseMessage response, string responseData, Exception ex)
        {
            throw new ClientException("Could not deserialize the response body.",
                (int)response.StatusCode,
                responseData,
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

    }
}