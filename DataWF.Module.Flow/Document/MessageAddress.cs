﻿/*
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

namespace DataWF.Module.Flow
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
    [Table("flow", "maddress")]
    public class MessageAddress : DBItem
    {
        public static DBTable<MessageAddress> DBTable
        {
            get { return DBService.GetTable<MessageAddress>(); }
        }

        public MessageAddress()
        {
            Build(DBTable);
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetProperty<int?>(nameof(Id)); }
            set { SetProperty(value, nameof(Id)); }
        }

        [Browsable(false)]
        [Column("messageid")]
        public int? MessageId
        {
            get { return GetProperty<int?>(nameof(MessageId)); }
            set { SetProperty(value, nameof(MessageId)); }
        }

        [Reference("fk_maddress_messageid", nameof(MessageId))]
        public Message Message
        {
            get { return GetPropertyReference<Message>(nameof(MessageId)); }
            set { SetPropertyReference(value, nameof(MessageId)); }
        }

        [Browsable(false)]
        [Column("userid")]
        public int? UserId
        {
            get { return GetProperty<int?>(nameof(UserId)); }
            set { SetProperty(value, nameof(UserId)); }
        }

        [Reference("fk_maddress_userid", nameof(UserId))]
        public User User
        {
            get { return GetPropertyReference<User>(nameof(UserId)); }
            set { SetPropertyReference(value, nameof(UserId)); }
        }

        [Column("dateread")]
        public DateTime? DateRead
        {
            get { return GetProperty<DateTime?>(nameof(DateRead)); }
            set { SetProperty(value, nameof(DateRead)); }
        }
    }

}