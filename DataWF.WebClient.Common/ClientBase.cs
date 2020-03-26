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

        private string baseUrl;
        private IClientProvider provider;

        public ClientBase()
        {
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

        public virtual HttpClient GetHttpClient()
        {
            return Provider.CreateHttpClient();
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
            return Helper.GetDocumentsFullPath(fileName, indentifier);
        }

        public virtual async Task<R> Request<R>(ProgressToken progressToken,
            string httpMethod = "GET",
            string commandUrl = "/api",
            string mediaType = "application/json",
            object value = null,
            params object[] routeParams)
        {
            var client = GetHttpClient();
            try
            {
                using (var request = CreateRequest(progressToken, httpMethod, commandUrl, mediaType, value, routeParams))
                {
                    System.Diagnostics.Debug.WriteLine(request.RequestUri);

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
                                            using (var reader = new StreamReader(encodedStream))
                                            {
                                                result = (R)(object)reader.ReadToEnd();
                                            }
                                        }
                                        else
                                        {
#if NETSTANDARD2_0
                                            var jsonSerializer = Newtonsoft.Json.JsonSerializer.Create(Provider.JsonSettings);
                                            using (var sreader = new StreamReader(encodedStream))
                                            using (var jreader = new Newtonsoft.Json.JsonTextReader(sreader))
                                            {
                                                result = (R)jsonSerializer.Deserialize(jreader, typeof(R));
                                            }
#else
                                            result = await System.Text.Json.JsonSerializer.DeserializeAsync<R>(encodedStream, Provider.JsonSettings).ConfigureAwait(false);
#endif
                                            if (encodedStream.CanWrite)
                                            {
                                                using (var file = File.OpenWrite("output.json"))
                                                    encodedStream.CopyTo(file);
                                            }
                                        }
                                    }
                                }
                                break;
                            case System.Net.HttpStatusCode.Unauthorized:
                                if (await Provider?.OnUnauthorized())
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

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(ParseUrl(commandUrl, parameters).ToString(), UriKind.RelativeOrAbsolute),
            };

            if (httpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                Status = ClientStatus.Get;
                request.Method = HttpMethod.Get;
            }
            else if (httpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                Status = ClientStatus.Post;
                request.Method = HttpMethod.Post;
            }
            else if (httpMethod.Equals("PUT", StringComparison.OrdinalIgnoreCase))
            {
                Status = ClientStatus.Put;
                request.Method = HttpMethod.Put;
            }
            else if (httpMethod.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                Status = ClientStatus.Delete;
                request.Method = HttpMethod.Delete;
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
                Validation(value);
#if NETSTANDARD2_0
                string text;
                var serializer = Newtonsoft.Json.JsonSerializer.Create(Provider.JsonSettings);
                using (var writer = new StringWriter())
                using (var jwriter = new Newtonsoft.Json.JsonTextWriter(writer))
                {
                    serializer.Serialize(jwriter, value);
                    jwriter.Flush();
                    text = writer.ToString();
                }
#else
                var text = System.Text.Json.JsonSerializer.Serialize(value, value.GetType(), Provider.JsonSettings);
#endif
                request.Content = new StringContent(text, Encoding.UTF8, "application/json");
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

                urlBuilder.Append(Uri.EscapeDataString(ConvertToString(parameters[i++], CultureInfo.InvariantCulture)));

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