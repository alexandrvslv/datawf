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

namespace DataWF.Module.Common
{

    [DataContract, Table("rinstance", "User", BlockSize = 100)]
    public class Instance : DBItem
    {
        private static DBColumn userKey = DBColumn.EmptyKey;
        private static DBColumn hostKey = DBColumn.EmptyKey;
        private static DBColumn portKey = DBColumn.EmptyKey;
        private static DBColumn activeKey = DBColumn.EmptyKey;
        private static DBTable<Instance> dbTable;

        public static DBColumn UserKey => DBTable.ParseProperty(nameof(UserId), userKey);
        public static DBColumn HostKey => DBTable.ParseProperty(nameof(Host), hostKey);
        public static DBColumn PortKey => DBTable.ParseProperty(nameof(Port), portKey);
        public static DBColumn ActiveKey => DBTable.ParseProperty(nameof(Active), activeKey);
        public static DBTable<Instance> DBTable => dbTable ?? (dbTable = GetTable<Instance>());

        public static Instance GetByNetId(IPEndPoint endPoint, bool create)
        {
            var query = new QQuery(DBTable);
            query.BuildPropertyParam(nameof(Host), CompareType.Equal, endPoint.Address.ToString());
            query.BuildPropertyParam(nameof(Port), CompareType.Equal, endPoint.Port);
            query.BuildPropertyParam(nameof(Action), CompareType.Equal, false);
            var instance = DBTable.Load(query).FirstOrDefault();
            if (instance == null && create)
            {
                instance = new Instance
                {
                    EndPoint = endPoint,
                    User = User.CurrentUser,
                    Active = true,
                    IsCurrent = true
                };
                instance.Save();
            }
            return instance;
        }

        private IPEndPoint ipEndPoint;

        public Instance()
        { }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("user_id"), Browsable(false)]
        public int? UserId
        {
            get { return GetValue<int?>(UserKey); }
            set { SetValue(value, UserKey); }
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get { return GetReference<User>(UserKey); }
            set { SetReference(value, UserKey); }
        }

        [DataMember, Column("instance_host")]
        public string Host
        {
            get { return GetValue<string>(HostKey); }
            set { SetValue(value, HostKey); }
        }

        [DataMember, Column("instance_port")]
        public int? Port
        {
            get { return GetValue<int?>(PortKey); }
            set { SetValue(value, PortKey); }
        }

        [DataMember, Column("instance_active")]
        public bool? Active
        {
            get { return GetValue<bool?>(ActiveKey); }
            set { SetValue(value, ActiveKey); }
        }

        public IPEndPoint EndPoint
        {
            get { return ipEndPoint ?? (ipEndPoint = Host == null ? null : new IPEndPoint(IPAddress.Parse(Host), Port.GetValueOrDefault())); }
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
