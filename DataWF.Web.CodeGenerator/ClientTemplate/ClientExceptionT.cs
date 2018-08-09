using System;
using System.Collections.Generic;

namespace NewNameSpace
{
    public partial class ClientException<TResult> : ClientException
    {
        public TResult Result { get; private set; }

        public ClientException(string message, int statusCode, string response, Dictionary<string, IEnumerable<string>> headers, TResult result, Exception innerException)
            : base(message, statusCode, response, headers, innerException)
        {
            Result = result;
        }
    }
}