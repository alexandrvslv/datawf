/*
 User.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>
 
 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
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

    [Table("rinstance", "User", BlockSize = 128)]
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
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("instance_host")]
        public string Host
        {
            get => GetValue<string>(HostKey);
            set => SetValue(value, HostKey);
        }

        [Column("instance_port")]
        public int? Port
        {
            get => GetValue<int?>(PortKey);
            set => SetValue(value, PortKey);
        }

        [Column("instance_active")]
        public bool? Active
        {
            get => GetValue<bool?>(ActiveKey);
            set => SetValue(value, ActiveKey);
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

        public bool IsCurrent { get; internal set; }

        public long Count { get; set; }

        [DefaultFormat("size")]
        public long Length { get; set; }

    }
}
