using System;
using System.ComponentModel;
using System.Net.Http;

namespace NewNameSpace
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

        public event EventHandler<CancelEventArgs> UnauthorizedError;

        public bool OnUnauthorizedError()
        {
            var cancel = new CancelEventArgs(true);
            UnauthorizedError?.Invoke(this, cancel);
            return !cancel.Cancel;
        }
    }
}