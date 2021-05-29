//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

using DataWF.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [InvokerGenerator]
    public partial class RSInstance : IEquatable<RSInstance>
    {
        private readonly ManualResetEventSlim requestEvent = new ManualResetEventSlim(false);
        private IPEndPoint endPoint;
        private static readonly int timeOut = 60000;

        public string Host { get; set; } = "localhost";

        public int Port { get; set; } = 50001;

        [XmlIgnore, JsonIgnore]
        public IPEndPoint EndPoint => endPoint ?? (endPoint = new IPEndPoint(SocketHelper.ParseIPAddress(Host), Port));

        [XmlIgnore, JsonIgnore]
        public string EndPointName { get => EndPoint.ToString(); }

        [XmlIgnore, JsonIgnore]
        public bool? Active { get; internal set; }

        [XmlIgnore, JsonIgnore]
        public TcpSocket TcpSocket { get; internal set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as RSInstance);
        }

        public bool Equals(RSInstance other)
        {
            return other != null &&
                   Host == other.Host &&
                   Port == other.Port;
        }

        public override int GetHashCode()
        {
            int hashCode = 995452845;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Host);
            hashCode = hashCode * -1521134295 + Port.GetHashCode();
            return hashCode;
        }
        public async Task<SMResponce> Request(SMRequest request, ReplicationService service)
        {
            if (!(Active ?? false))
                throw new Exception("Inactive recipient");

            requestEvent.Reset();
            if (await Send(request, service))
            {
                requestEvent.Wait(timeOut);
                return request.Responce;
            }
            return null;
        }

        public async Task<bool> Send<T>(T message, ReplicationService service) where T : SMBase
        {
            var sended = await service.SendElement(EndPoint, message);
            if (sended)
            {
                if (message is SMRequest request)
                {
                    requests[request.Id] = request;
                }
            }
            return false;
        }

    }
}
