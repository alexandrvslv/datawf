using System;
using System.Net.Http;

namespace DataWF.Web.Client
{
    public class AuthorizationInfo
    {
        public string Url { get; set; }
        public string Key { get; set; }
        public string Token { get; set; }

        public void FillRequest(HttpRequestMessage request)
        {
            if (Token != null)
            {
                request.Headers.Add("Authorization", $"{Key} {Token}");
            }
        }
    }
}