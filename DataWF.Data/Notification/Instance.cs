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
    [Table("rinstance", "General", BlockSize = 128)]
    public class Instance : DBItem, IInstance
    {
        public static readonly DBTable<Instance> DBTable = GetTable<Instance>();
        public static readonly DBColumn HostKey = DBTable.ParseProperty(nameof(Host));
        public static readonly DBColumn PortKey = DBTable.ParseProperty(nameof(Port));
        public static readonly DBColumn ActiveKey = DBTable.ParseProperty(nameof(Active));

        public static async Task<Instance> GetByNetId(IPEndPoint endPoint, bool create, IUserIdentity user = null)
        {
            var query = new QQuery(DBTable);
            query.BuildPropertyParam(nameof(Host), CompareType.Equal, endPoint.Address.ToString());
            query.BuildPropertyParam(nameof(Port), CompareType.Equal, endPoint.Port);
            query.BuildPropertyParam(nameof(Action), CompareType.Equal, false);
            var instance = DBTable.Load(query).LastOrDefault();
            if (instance == null && create)
            {
                instance = new Instance
                {
                    EndPoint = endPoint,
                    Active = true,
                    IsCurrent = true
                };
                await instance.Save(user);
            }
            return instance;
        }

        private IPEndPoint ipEndPoint;

        public Instance()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValueNullable<int>(Table.PrimaryKey);
            set => SetValueNullable(value, Table.PrimaryKey);
        }

        [Column("instance_host", Keys = DBColumnKeys.View)]
        public string Host
        {
            get => GetValue<string>(HostKey);
            set => SetValue(value, HostKey);
        }

        [Column("instance_port", Keys = DBColumnKeys.View)]
        public int? Port
        {
            get => GetValueNullable<int>(PortKey);
            set => SetValueNullable(value, PortKey);
        }

        [Column("instance_active")]
        public bool? Active
        {
            get => GetValueNullable<bool>(ActiveKey);
            set => SetValueNullable(value, ActiveKey);
        }

        [JsonIgnore, XmlIgnore]
        public IPEndPoint EndPoint
        {
            get => ipEndPoint ?? (ipEndPoint = Host == null ? null : new IPEndPoint(IPAddress.Parse(Host), Port.GetValueOrDefault()));
            set
            {
                ipEndPoint = value;
                Host = value?.Address.ToString();
                Port = value?.Port;
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
            return $"{EndPoint} Sent: {SendCount}({Helper.LenghtFormat(SendLength)}) Receive: {ReceiveCount}({ReceiveLength})";
        }
    }
}
