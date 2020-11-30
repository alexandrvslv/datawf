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
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class SRInstance : IEquatable<SRInstance>
    {
        private IPEndPoint endPoint;

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
            return Equals(obj as SRInstance);
        }

        public bool Equals(SRInstance other)
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


    }
}
