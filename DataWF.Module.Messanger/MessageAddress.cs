/*
 Message.cs
 
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
using DataWF.Module.Common;
using System.Runtime.Serialization;

namespace DataWF.Module.Messanger
{
    public class MessageAddressList : DBTableView<MessageAddress>
    {
        public MessageAddressList(string filter)
            : base(MessageAddress.DBTable, filter)
        {
            //_ApplySort(new DBRowComparer(FlowEnvir.Config.StageParameter.Table, FlowEnvir.Config.StageParameter.Table.PrimaryKey.Code, ListSortDirection.Ascending));
        }

        public MessageAddressList()
            : this(string.Empty)
        { }

        public MessageAddressList(Message message)
            : this(string.Format("({0} = {1}",
                                 MessageAddress.DBTable.ParseProperty(nameof(MessageAddress.MessageId)).Name, message.PrimaryId))
        { }
    }

    [DataContract, Table("dmessage_address", "Message", IsLoging = false)]
    public class MessageAddress : MessageDetail
    {
        public static DBTable<MessageAddress> DBTable
        {
            get { return GetTable<MessageAddress>(); }
        }

        public MessageAddress()
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Browsable(false)]
        [DataMember, Column("user_id"), Index("dmessage_address_user_id")]
        public int? UserId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get { return GetPropertyReference<User>(); }
            set
            {
                SetPropertyReference(value);
                Position = value?.Position;
            }
        }

        [Browsable(false)]
        [DataMember, Column("position_id", Keys = DBColumnKeys.View), Index("dmessage_address_position_id")]
        public int? PositionId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get { return GetPropertyReference<Position>(); }
            set
            {
                SetPropertyReference(value);
                Department = value?.Department;
            }
        }

        [Browsable(false)]
        [DataMember, Column("department_id", Keys = DBColumnKeys.View), Index("dmessage_address_department_id")]
        public int? DepartmentId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get { return GetPropertyReference<Department>(); }
            set { SetPropertyReference(value); }
        }

        [DataMember, Column("date_read")]
        public DateTime? DateRead
        {
            get { return GetProperty<DateTime?>(); }
            set { SetProperty(value); }
        }

        [Browsable(false)]
        public DBItem Staff
        {
            get { return (DBItem)User ?? (DBItem)Position ?? (DBItem)Department; }
            set
            {
                if (value is Department)
                    Department = (Department)value;
                else if (value is Position)
                    Position = (Position)value;
                else if (value is User)
                    User = (User)value;
            }
        }
    }

}