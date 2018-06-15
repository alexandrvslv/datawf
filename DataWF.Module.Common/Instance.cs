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
using DataWF.Data;
using DataWF.Common;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Net;

namespace DataWF.Module.Common
{
    [DataContract, Table("rinstance", "User", BlockSize = 100)]
    public class Instance : DBItem
    {
        public static DBTable<Instance> DBTable { get { return GetTable<Instance>(); } }

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
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("user_id"), Browsable(false)]
        public int? UserId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get { return GetPropertyReference<User>(); }
            set { SetPropertyReference(value); }
        }

        [DataMember, Column("instance_host")]
        public string Host
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("instance_port")]
        public int? Port
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("instance_active")]
        public bool? Active
        {
            get { return GetProperty<bool?>(); }
            set { SetProperty(value); }
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
