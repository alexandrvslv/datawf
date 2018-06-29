using DataWF.Common;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
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
    public abstract partial class BaseClient<T, K> : IBaseClient
    {
        private Lazy<JsonSerializerSettings> _settings;
        private string baseUrl;

        public BaseClient()
        {
            _settings = new Lazy<JsonSerializerSettings>(() =>
            {
                var settings = new JsonSerializerSettings();
                UpdateJsonSerializerSettings(settings);
                return settings;
            });
        }

        protected JsonSerializerSettings JsonSerializerSettings { get { return _settings.Value; } }

        public IBaseProvider Provider { get; set; }

        public string BaseUrl
        {
            get { return Provider?.BaseUrl ?? baseUrl; }
            set { baseUrl = value; }
        }

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
                    if (httpMethod == "POST" || httpMethod == "PUT")
                    {
                        var content_ = new StringContent(JsonConvert.SerializeObject(value, _settings.Value));
                        content_.Headers.ContentType = MediaTypeHeaderValue.Parse(mediaType);
                        request.Content = content_;
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
                        var headers_ = Enumerable.ToDictionary(response.Headers, h_ => h_.Key, h_ => h_.Value);
                        if (response.Content != null && response.Content.Headers != null)
                        {
                            foreach (var item_ in response.Content.Headers)
                                headers_[item_.Key] = item_.Value;
                        }

                        ProcessResponse(client, response);

                        var status = response.StatusCode;
                        if (status == System.Net.HttpStatusCode.OK)
                        {
                            var responseData = response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (responseData != null)
                            {
                                try
                                {
                                    return JsonConvert.DeserializeObject<R>(responseData, _settings.Value);
                                }
                                catch (Exception exception_)
                                {
                                    throw new ClientException("Could not deserialize the response body.", (int)response.StatusCode, responseData, headers_, exception_);
                                }
                            }
                        }
                        else
                        if (status != System.Net.HttpStatusCode.OK && status != System.Net.HttpStatusCode.NoContent)
                        {
                            var responseData_ = response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            throw new ClientException("The HTTP status code of the response was not expected (" + (int)response.StatusCode + ").", (int)response.StatusCode, responseData_, headers_, null);
                        }

                        return default(R);
                    }
                }
            }
        }

        public abstract Task<List<T>> GetAsync();

        public abstract Task<List<T>> GetAsync(CancellationToken cancellationToken);

        Task IBaseClient.GetAsync() { return GetAsync(); }

        public abstract Task<T> PutAsync(T value);

        public abstract Task<T> PutAsync(T value, CancellationToken cancellationToken);

        public Task PutAsync(object value) { return PutAsync((K)value); }

        public abstract Task<T> PostAsync(T value);

        public abstract Task<T> PostAsync(T value, CancellationToken cancellationToken);

        public Task PostAsync(object value) { return PostAsync((K)value); }

        public abstract Task<List<T>> FindAsync(string filter);

        public abstract Task<List<T>> FindAsync(string filter, CancellationToken cancellationToken);

        Task IBaseClient.FindAsync(string filter) { return FindAsync(filter); }

        public abstract Task<T> GetAsync(K id);

        public abstract Task<T> GetAsync(K id, CancellationToken cancellationToken);

        public Task GetAsync(object id) { return GetAsync((K)id); }

        public abstract Task<K> DeleteAsync(K id);

        public abstract Task<K> DeleteAsync(K id, CancellationToken cancellationToken);

        public Task DeleteAsync(object id) { return DeleteAsync((K)id); }

        protected string ConvertToString(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value is System.Enum)
            {
                string name = System.Enum.GetName(value.GetType(), value);
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
                return System.Convert.ToBase64String((byte[])value);
            }
            else if (value.GetType().IsArray)
            {
                var array = System.Linq.Enumerable.OfType<object>((System.Array)value);
                return string.Join(",", System.Linq.Enumerable.Select(array, o => ConvertToString(o, cultureInfo)));
            }

            return System.Convert.ToString(value, cultureInfo);
        }


    }
}