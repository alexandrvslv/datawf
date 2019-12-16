﻿using Brotli;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly Lazy<JsonSerializerSettings> serializeSettings;
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
                //var handler = new HttpClientHandler()
                //{
                //    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                //};
                client = new HttpClient() { Timeout = TimeSpan.FromHours(1) };
            }
            return client;
        }

        protected virtual void Validation(object value)
        {
            var vc = new ValidationContext(value);
            //var results = new List<ValidationResult>(); 
            Validator.ValidateObject(value, vc, true);
        }

        public string GetFilePath(IFileModel fileModel, string commandUrl)
        {
            if (fileModel is IPrimaryKey key)
            {
                return GetFilePath(fileModel.FileName, commandUrl, key.PrimaryKey);
            }
            return null;
        }

        public string GetFilePath(string fileName, string commandUrl, params object[] parameters)
        {
            var uri = new Uri(ParseUrl(commandUrl, parameters).ToString(), UriKind.RelativeOrAbsolute);
            return GetFilePath(fileName, uri);
        }

        public string GetFilePath(string fileName, Uri uri)
        {
            var indentifier = uri.LocalPath.Replace("/", "");
            return Helper.GetDocumentsFullPath(fileName, indentifier);
        }

        public virtual async Task<R> Request<R>(ProgressToken progressToken,
            string httpMethod = "GET",
            string commandUrl = "/api",
            string mediaType = "application/json",
            object value = null,
            params object[] routeParams)
        {
            var client = CreateHttpClient();
            try
            {
                using (var request = CreateRequest(progressToken, httpMethod, commandUrl, mediaType, value, routeParams))
                {
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
                                    using (var encodedStream = GetEncodedStream(response, responseStream))
                                    using (var reader = new StreamReader(encodedStream))
                                    {
                                        if (mediaType.Equals("application/octet-stream"))
                                        {
                                            var headers = GetHeaders(response);
                                            (string fileName, int fileSize) = GetFileInfo(headers);
                                            var filePath = GetFilePath(fileName, request.RequestUri);
                                            var fileStream = (FileStream)null;
                                            try
                                            {
                                                fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                                            }
                                            catch (IOException ioex)
                                            {
                                                if (ioex.HResult == -2147024864)
                                                {
                                                    throw new Exception($"File {fileName} is already open!\r\nPlease close the application that is blocking the file.\r\n And try to download again.");
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
                                if (await Provider?.Authorization?.OnUnauthorizedError())
                                {
                                    return await Request<R>(progressToken, httpMethod, commandUrl, mediaType, value, routeParams).ConfigureAwait(false);
                                }
                                else
                                {
                                    ErrorStatus("Unauthorized! Try Relogin!", response, await ReadContentAsString(response));
                                }
                                break;
                            case System.Net.HttpStatusCode.Forbidden:
                                var details = string.Empty;

                                ErrorStatus("Access Denied!", response, await ReadContentAsString(response));
                                break;
                            case System.Net.HttpStatusCode.NotFound:
                                ErrorStatus("No Data Found!", response, await ReadContentAsString(response));
                                break;
                            case System.Net.HttpStatusCode.BadRequest:
                                BadRequest(await ReadContentAsString(response), response);
                                break;
                            case System.Net.HttpStatusCode.NoContent:
                                result = default(R);
                                break;
                            default:
                                UnexpectedStatus(response, await ReadContentAsString(response));
                                break;
                        }
                        Status = ClientStatus.Compleate;
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                if (progressToken.IsCancelled)
                {
                    Helper.OnException(ex);
                    return (R)(object)null;
                }
                else
                {
                    throw ex;
                }
            }
        }

        private static async Task<string> ReadContentAsString(HttpResponseMessage response)
        {
            if (response.Content == null)
                return null;
            using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                using (var encoded = GetEncodedStream(response, responseStream))
                using (var stringReader = new StreamReader(encoded))
                {
                    return await stringReader.ReadToEndAsync();
                }
            }
        }

        private static Stream GetEncodedStream(HttpResponseMessage response, Stream responseStream)
        {
            if (response.Content.Headers.TryGetValues("Content-Encoding", out var encodedBy))
            {
                if (encodedBy.Contains("br", StringComparer.OrdinalIgnoreCase)
                    || encodedBy.Contains("brotli", StringComparer.OrdinalIgnoreCase))
                    return new Brotli.BrotliStream(responseStream, CompressionMode.Decompress, false);
                if (encodedBy.Contains("gzip", StringComparer.OrdinalIgnoreCase))
                    return new GZipStream(responseStream, CompressionMode.Decompress, false);
                if (encodedBy.Contains("deflate", StringComparer.OrdinalIgnoreCase))
                    return new DeflateStream(responseStream, CompressionMode.Decompress, false);
            }
            return responseStream;
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
            try
            {
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
                                    using (var encodedStream = GetEncodedStream(response, responseStream))
                                    using (var reader = new StreamReader(encodedStream))
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
                                if (await Provider?.Authorization?.OnUnauthorizedError())
                                {
                                    return await RequestArray<R, I>(progressToken, httpMethod, commandUrl, mediaType, value, parameters).ConfigureAwait(false);
                                }
                                else
                                {
                                    ErrorStatus("Unauthorized! Try Relogin!", response, await ReadContentAsString(response));
                                }
                                break;
                            case System.Net.HttpStatusCode.NotFound:
                                ErrorStatus("No Data Found!", response, await ReadContentAsString(response));
                                break;
                            case System.Net.HttpStatusCode.BadRequest:
                                BadRequest(await ReadContentAsString(response), response);
                                break;
                            case System.Net.HttpStatusCode.NoContent:
                                result = default(R);
                                break;
                            default:
                                UnexpectedStatus(response, await ReadContentAsString(response));
                                break;
                        }
                        Status = ClientStatus.Compleate;
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                if (progressToken.IsCancelled)
                {
                    Helper.OnException(ex);
                    return (R)(object)null;
                }
                else
                {
                    throw ex;
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

        protected virtual void SerializeArray(JsonSerializer serializer, JsonTextWriter jwriter, IList list)
        {
            jwriter.WriteStartArray();
            var itemType = TypeHelper.GetItemType(list);
            var itemInfo = Serialization.Instance.GetTypeInfo(itemType);
            foreach (var item in list)
            {
                if (item is ISynchronized isSynch && isSynch.SyncStatus == SynchronizedStatus.Actual)
                {
                    continue;
                }
                SerializeValue(serializer, jwriter, item, itemInfo);
            }
            jwriter.WriteEndArray();
        }

        protected virtual IList DeserializeArray(JsonSerializer serializer, JsonTextReader jreader, Type type, IList sourceList)
        {
            if (type == null)
            {
                while (jreader.Read() && jreader.TokenType != JsonToken.EndArray)
                { }
                return null;
            }
            var itemType = TypeHelper.GetItemType(type);
            var client = Provider.GetClient(itemType);
            var temp = sourceList ?? (IList)EmitInvoker.CreateObject(type);
            lock (temp)
            {
                var referenceList = temp as IReferenceList;
                if (referenceList != null
                    && referenceList.Owner.SyncStatus == SynchronizedStatus.Load)
                {
                    foreach (var item in referenceList.TypeOf<ISynchronized>())
                    {
                        item.SyncStatus = SynchronizedStatus.Load;
                    }
                }
                else
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

                if (referenceList != null
                    && referenceList.Owner.SyncStatus == SynchronizedStatus.Load
                    && client != null)
                {
                    for (var i = 0; i < referenceList.Count; i++)
                    {
                        var item = referenceList[i];
                        if (item is ISynchronized synched
                            && synched.SyncStatus == SynchronizedStatus.Load)
                        {

                            if (!client.Remove(item))
                            {
                                referenceList.RemoveAt(i);
                            }
                            i--;
                        }
                    }
                }
            }
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

        public virtual void SerializeObject(JsonSerializer serializer, JsonTextWriter jwriter, object item, TypeSerializationInfo info = null)
        {
            var type = item.GetType();
            var typeInfo = info?.Type == type ? info : Serialization.Instance.GetTypeInfo(type);
            var synched = item as ISynchronized;

            jwriter.WriteStartObject();
            foreach (var property in typeInfo.Properties)
            {
                if (!property.IsWriteable
                    || (property.IsChangeSensitive
                    && synched != null
                    && !(synched.Changes.ContainsKey(property.Name))))
                {
                    continue;
                }

                jwriter.WritePropertyName(property.Name);
                var value = property.Invoker.GetValue(item);
                if (property.IsAttribute || value == null)
                {
                    serializer.Serialize(jwriter, value);
                }
                else if (value is IList list)
                {
                    SerializeArray(serializer, jwriter, list);
                }
                else
                {
                    SerializeObject(serializer, jwriter, value);
                }
            }
            jwriter.WriteEndObject();
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

        public void SerializeValue(JsonSerializer serializer, JsonTextWriter jwriter, object item, TypeSerializationInfo info = null)
        {
            var type = item?.GetType();
            if (type == null || (info?.IsAttribute ?? TypeHelper.IsSerializeAttribute(type)))
            {
                serializer.Serialize(jwriter, item);
            }
            else if (item is IList list)
            {
                SerializeArray(serializer, jwriter, list);
            }
            else
            {
                SerializeObject(serializer, jwriter, item, info);
            }
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
                var fileStream = stream as FileStream;
                var fileName = parameters.Length > 1 ? (string)parameters[1]
                    : fileStream != null ? Path.GetFileName(fileStream.Name)
                    : "somefile.ext";
                var lastWriteTime = fileStream != null ? File.GetLastWriteTimeUtc(fileStream.Name) : DateTime.UtcNow;
                var content = new MultipartFormDataContent
                {
                    { new StringContent(lastWriteTime.ToString("o")), "LastWriteTime" },
                    { new ProgressStreamContent(progressToken, stream, 81920), Path.GetFileNameWithoutExtension(fileName), fileName }
                };
                request.Content = content;
            }
            else if (value != null)
            {
                Validation(value);
                var serializer = JsonSerializer.Create(JsonSerializerSettings);
                using (var writer = new StringWriter())
                using (var jwriter = new JsonTextWriter(writer))
                {
                    SerializeValue(serializer, jwriter, value);
                    jwriter.Flush();
                    var contentText = writer.ToString();
                    var content = new StringContent(contentText, Encoding.UTF8);
                    content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                    request.Content = content;
                }
            }
            if (httpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
            }
            if (mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
            }
            return request;
        }

        protected virtual StringBuilder ParseUrl(string url, params object[] parameters)
        {
            var urlBuilder = new StringBuilder();
            urlBuilder.Append(BaseUrl?.TrimEnd('/') ?? "");
            int i = 0;
            int c = 0;
            foreach (Match m in Regex.Matches(url, @"{.[^}]*}", RegexOptions.Compiled))
            {
                if (m.Index > c)
                {
                    urlBuilder.Append(url.Substring(c, m.Index - c));
                }
                if (parameters.Length <= i)
                {
                    throw new ArgumentException();
                }

                urlBuilder.Append(Uri.EscapeDataString(ConvertToString(parameters[i++], CultureInfo.InvariantCulture)));

                c = m.Index + m.Length;
            }
            if (c < url.Length)
            {
                urlBuilder.Append(url.Substring(c));
            }
            return urlBuilder;
        }

        protected string ConvertToString(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value is Enum)
            {
                return EnumItem.Format(value);
            }
            else if (value is byte[])
            {
                return Convert.ToBase64String((byte[])value);
            }
            else if (TypeHelper.IsEnumerable(value.GetType()))
            {
                var array = value as IEnumerable;
                return string.Join(",", array.Cast<object>().Select(o => ConvertToString(o, cultureInfo)));
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
            throw new ClientException(message,
                (int)response.StatusCode,
                responseData,
                GetHeaders(response), null);
        }

        private void BadRequest(string message, HttpResponseMessage response)
        {
            message = message.Trim('\"').Replace("\\r", "\r").Replace("\\n", "\n");
            throw new ClientException(message,
                (int)response.StatusCode,
                null,
                GetHeaders(response), null);
        }

    }
}