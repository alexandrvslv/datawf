using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;

namespace DataWF.Common
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

        public Func<Task<bool>> UnauthorizedError;

        public async Task<bool> OnUnauthorizedError()
        {
            if (UnauthorizedError != null)
            {
                return await UnauthorizedError();
            }
            return false;
        }
    }
}