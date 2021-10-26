using System;
using System.Collections.Generic;

namespace DataWF.Common
{
    public partial class ClientException : System.Exception
    {
        public int StatusCode { get; private set; }

        public string Response { get; private set; }

        public Dictionary<string, IEnumerable<string>> Headers { get; private set; }

        public ClientException(string message, int statusCode, string response, Dictionary<string, IEnumerable<string>> headers, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            Response = response;
            Headers = headers;
        }

        public override string ToString()
        {
            return string.Format("HTTP Response: \n\n{0}\n\n{1}", Response, base.ToString());
        }
    }
}