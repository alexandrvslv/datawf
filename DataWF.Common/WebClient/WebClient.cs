using Brotli;
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
    public partial class WebClient : IWebClient
    {
        private const string fileNameUTFToken = "filename*=UTF-8";
        private const string fileNameToken = "filename=";

        private string baseUrl;
        private IWebSchema schema;
        private Environment.SpecialFolder defaultFolder = Environment.SpecialFolder.LocalApplicationData;

        public WebClient()
        {
        }
        
        public string Name { get; set; }

        public IWebSchema Schema
        {
            get => schema;
            set => Initialize(value);
        }

        public IModelProvider Provider => Schema.Provider;

        protected virtual void Initialize(IWebSchema schema)
        {
            this.schema = schema;
        }

        public WebClientStatus Status { get; set; }

        public string BaseUrl
        {
            get { return Schema?.BaseUrl ?? baseUrl; }
            set { baseUrl = value; }
        }

        public event EventHandler CacheCleared;

        public virtual HttpClient GetHttpClient()
        {
            return Schema.GetHttpClient();
        }

        public virtual void ClearCache()
        {
            CacheCleared?.Invoke(this, EventArgs.Empty);
        }

        partial void ProcessResponse(HttpClient client, HttpResponseMessage response);

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
            return Helper.GetDocumentsFullPath(fileName, indentifier, defaultFolder);
        }

        public virtual async Task<R> Request<R>(ProgressToken progressToken,
            HttpMethod httpMethod,
            string commandUrl,// = "/api"
            string mediaType,// = "application/json"            
            object value,
            params object[] routeParams)
        {
            var client = GetHttpClient();
            var pages = progressToken.Pages;
            try
            {
                using (var request = CreateRequest(progressToken, httpMethod, commandUrl, mediaType, value, routeParams))
                {
                    System.Diagnostics.Debug.WriteLine($"{request.RequestUri} {value}");

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
                                if (value is IList list && value.GetType() == typeof(R))
                                {
                                    foreach (var synchedItem in list.OfType<ISynchronized>())
                                    {
                                        synchedItem.SyncStatus = SynchronizedStatus.Load;
                                    }
                                }
                                if (value is IFileModel fileModel
                                    && (fileModel.FileWatcher?.IsChanged ?? false)
                                    && request.Content is MultipartFormDataContent)
                                {
                                    fileModel.FileWatcher.IsChanged = false;
                                }
                                using (var responseStream = response.Content == null ? null : await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                                {
                                    using (var encodedStream = GetEncodedStream(response, responseStream))
                                    {
                                        if (mediaType.Equals("application/octet-stream"))
                                        {
                                            var headers = GetHeaders(response);
                                            (string fileName, int fileSize) = GetFileInfo(headers);
                                            var filePath = GetFilePath(fileName, request.RequestUri);
                                            var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
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
                                            using (var reader = new StreamReader(encodedStream))
                                            {
                                                result = (R)(object)reader.ReadToEnd();
                                            }
                                        }
                                        else
                                        {
                                            if (pages != null)
                                            {
                                                ReadPageSettings(response, pages);
                                            }
#if NETSTANDARD2_0
                                            var jsonSerializer = Newtonsoft.Json.JsonSerializer.Create(Schema.JsonSettings);
                                            using (var sreader = new StreamReader(encodedStream))
                                            using (var jreader = new Newtonsoft.Json.JsonTextReader(sreader))
                                            {
                                                result = (R)jsonSerializer.Deserialize(jreader, typeof(R));
                                            }
#else
                                            result = await System.Text.Json.JsonSerializer.DeserializeAsync<R>(encodedStream, Schema.JsonSettings).ConfigureAwait(false);
#endif
                                            if (pages != null && pages.ListCount == 0
                                                && result is IList rlist && rlist.Count > 0)
                                            {
                                                pages.ListCount = rlist.Count;
                                            }
                                        }
                                    }
                                }
                                break;
                            case System.Net.HttpStatusCode.Unauthorized:
                                if (await Schema?.OnUnauthorized())
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
                                result = default;
                                break;
                            default:
                                UnexpectedStatus(response, await ReadContentAsString(response));
                                break;
                        }
                        Status = WebClientStatus.Compleate;
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

        private static void ReadPageSettings(HttpResponseMessage response, HttpPageSettings pages)
        {
            IEnumerable<string> values;
            if (response.Headers.TryGetValues(HttpPageSettings.XListCount, out values)
                && int.TryParse(values.FirstOrDefault(), out var countValue))
            {
                pages.ListCount = countValue;
            }
            if (response.Headers.TryGetValues(HttpPageSettings.XPageIndex, out values)
                && int.TryParse(values.FirstOrDefault(), out var pageIndex))
            {
                pages.PageIndex = pageIndex;
            }
            //if (response.Headers.TryGetValues(HttpPageSettings.XPageSize, out values)
            //    && int.TryParse(values.FirstOrDefault(), out var pageLegth))
            //{
            //    pages.PageSize = pageLegth;
            //}
            if (response.Headers.TryGetValues(HttpPageSettings.XPageCount, out values)
                && int.TryParse(values.FirstOrDefault(), out var pageCount))
            {
                pages.PageCount = pageCount;
            }
        }

        public static async Task<string> ReadContentAsString(HttpResponseMessage response)
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

        public static Stream GetEncodedStream(HttpResponseMessage response, Stream responseStream)
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
            var index = fileName.IndexOf(fileNameUTFToken, StringComparison.Ordinal);
            if (index > 0)
                fileName = fileName.Substring(index + fileNameUTFToken.Length).Trim(' ', '\"', '\'');
            var fileSize = 0;
            if (headers.TryGetValue("Content-Length", out var length))
            {
                int.TryParse(length.FirstOrDefault(), out fileSize);
            }
            return (fileName, fileSize);
        }

        public Dictionary<string, IEnumerable<string>> GetHeaders(HttpResponseMessage response)
        {
            var headers = Enumerable.ToDictionary(response.Headers, h => h.Key, h => h.Value, StringComparer.Ordinal);
            if (response.Content != null && response.Content.Headers != null)
            {
                foreach (var item in response.Content.Headers)
                    headers[item.Key] = item.Value;
            }
            return headers;
        }

        public virtual HttpRequestMessage CreateRequest(ProgressToken progressToken,
            HttpMethod httpMethod,
            string commandUrl,
            string mediaType,
            object value = null,
            params object[] parameters)
        {
            HttpJsonSettings jsonSettings = progressToken.JsonSettings;
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(ParseUrl(commandUrl, parameters).ToString(), UriKind.RelativeOrAbsolute),
                Method = httpMethod
            };
            request.Headers.Add(HttpJsonSettings.XJsonKeys, jsonSettings.Keys.ToString());
            request.Headers.Add(HttpJsonSettings.XJsonMaxDepth, jsonSettings.MaxDepth.ToString());
            if (progressToken.Pages is HttpPageSettings listSettings)
            {
                request.Headers.Add(HttpPageSettings.XListFrom, listSettings.ListFrom.ToString());
                request.Headers.Add(HttpPageSettings.XListTo, listSettings.ListTo.ToString());
            }
            if (httpMethod.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                Status = WebClientStatus.Get;
            }
            else if (httpMethod.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                Status = WebClientStatus.Post;
            }
            else if (httpMethod.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
            {
                Status = WebClientStatus.Put;
            }
            else if (httpMethod.Method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                Status = WebClientStatus.Delete;
            }

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
                IFileModel fileModel = null;
                FileStream fileStream = null;
                if (value is IFileModel
                   && commandUrl.IndexOf("UploadFileModel", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    fileModel = (IFileModel)value;
                    var content = new MultipartFormDataContent();

                    fileStream = fileModel.FileWatcher?.IsChanged ?? false ? fileModel.FileWatcher.OpenRead() : null;
                    if (fileStream != null)
                    {
                        fileModel.FileLastWrite = File.GetLastWriteTimeUtc(fileStream.Name);
                    }
                }
                string text;
#if NETSTANDARD2_0
                var serializer = Newtonsoft.Json.JsonSerializer.Create(Schema.JsonSettings);
                using (var writer = new StringWriter())
                using (var jwriter = new Newtonsoft.Json.JsonTextWriter(writer))
                {
                    NewtonJsonContractResolver.WriterContexts.TryAdd(jwriter, new ClientSerializationContext());
                    serializer.Serialize(jwriter, value);
                    jwriter.Flush();
                    text = writer.ToString();
                    NewtonJsonContractResolver.WriterContexts.TryRemove(jwriter, out _);
                }
#else
                using (var jstream = new MemoryStream())
                using (var jwriter = new System.Text.Json.Utf8JsonWriter(jstream,
                    new System.Text.Json.JsonWriterOptions { Encoder = Schema.JsonSettings.Encoder, Indented = Schema.JsonSettings.WriteIndented, SkipValidation = true }))
                {
                    SystemJsonConverterFactory.WriterContexts.TryAdd(jwriter, new ClientSerializationContext());
                    System.Text.Json.JsonSerializer.Serialize(jwriter, value, value.GetType(), Schema.JsonSettings);
                    jwriter.Flush();
                    text = Encoding.UTF8.GetString(jstream.ToArray());
                    SystemJsonConverterFactory.WriterContexts.TryRemove(jwriter, out _);
                }
#endif
                if (fileModel != null)
                {
                    var content = new MultipartFormDataContent();
                    content.Add(new StringContent(text, Encoding.UTF8, "application/json"), "Model");
                    if (fileStream != null)
                    {
                        var fileName = Path.GetFileName(fileStream.Name);
                        content.Add(new ProgressStreamContent(progressToken, fileStream, 81920), Path.GetFileNameWithoutExtension(fileName), fileName);
                    }
                    request.Content = content;
                }
                else
                {
                    request.Content = new StringContent(text, Encoding.UTF8, "application/json");
                }
            }
            if (request.Method == HttpMethod.Get)
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
            }
            if (mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))// && request.Version.Major < 2
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
                    throw new ArgumentOutOfRangeException(nameof(parameters));
                }
                var entry = ConvertToString(parameters[i++], CultureInfo.InvariantCulture);
                if (entry.Length > 0)
                {
                    urlBuilder.Append(Uri.EscapeDataString(entry));
                }
                else if (urlBuilder[urlBuilder.Length - 1] == '/')
                {
                    urlBuilder.Length -= 1;
                }
                c = m.Index + m.Length;
            }
            if (c < url.Length)
            {
                urlBuilder.Append(url.Substring(c));
            }
            return urlBuilder;
        }

        protected string ConvertToString(object value, CultureInfo cultureInfo)
        {
            if (value == null)
                return string.Empty;
            if (value is Enum)
            {
                return EnumItem.Format(value);
            }
            else if (value is byte[] bytes)
            {
                return Convert.ToBase64String(bytes);
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
            throw new ClientException(message, (int)response.StatusCode, responseData, GetHeaders(response), null);
        }

        private void BadRequest(string message, HttpResponseMessage response)
        {
            message = message.Trim('\"').Replace("\\r", "\r").Replace("\\n", "\n");
            throw new ClientException(message, (int)response.StatusCode, null, GetHeaders(response), null);
        }

        public virtual bool Add(object item)
        {
            return false;
        }

        public virtual bool Remove(object item)
        {
            return false;
        }
    }
}