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
using DataWF.Data;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [Table("rinstance", "General", BlockSize = 128, Keys = DBTableKeys.NoLogs | DBTableKeys.NoReplicate | DBTableKeys.Private, Type = typeof(InstanceTable))]
    [InvokerGenerator]
    public sealed partial class Instance : DBItem, IInstance
    {
        private IPEndPoint ipEndPoint;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("instance_url", 1024, Keys = DBColumnKeys.View)]
        public string Url
        {
            get => GetValue<string>(Table.UrlKey);
            set => SetValue(value, Table.UrlKey);
        }

        [Column("instance_active")]
        public bool? Active
        {
            get => GetValue<bool?>(Table.ActiveKey);
            set => SetValue(value, Table.ActiveKey);
        }

        [JsonIgnore, XmlIgnore]
        public IPEndPoint EndPoint
        {
            get => ipEndPoint ?? (ipEndPoint = Url == null ? null : new Uri(Url).ToEndPoint());
            set
            {
                ipEndPoint = value;
                Url = value?.ToStringUrl(UriScheme.udp);
            }
        }

        [JsonIgnore, XmlIgnore]
        public bool IsCurrent { get; internal set; }

        public long ReceiveCount { get; set; }

        [DefaultFormat("size")]
        public long ReceiveLength { get; set; }

        public long SendCount { get; set; }

        [DefaultFormat("size")]
        public long SendLength { get; set; }

        public override string ToString()
        {
            return $"{EndPoint} Sent: {SendCount}({Helper.SizeFormat(SendLength)}) Receive: {ReceiveCount}({ReceiveLength})";
        }
    }

    public partial class InstanceTable
    {
        public async Task<Instance> GetByNetId(IPEndPoint endPoint, bool create, IUserIdentity user = null)
        {
            var query = new QQuery(this);
            query.BuildParam(UrlKey, CompareType.Equal, endPoint.ToStringUrl(UriScheme.udp));
            query.BuildParam(ActiveKey, CompareType.Equal, false);
            var instance = Load(query).LastOrDefault();
            if (instance == null && create)
            {
                instance = new Instance(this)
                {
                    EndPoint = endPoint,
                    Active = true,
                    IsCurrent = true
                };
                await instance.Save(user);
            }
            return instance;
        }
    }

}

