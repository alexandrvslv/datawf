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

    [Table("wf_message", "dmessage_address", "Message")]
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
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Browsable(false)]
        [Column("messageid")]
        public int? MessageId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(MessageId))]
        public Message Message
        {
            get { return GetPropertyReference<Message>(); }
            set { SetPropertyReference(value); }
        }

        [Browsable(false)]
        [Column("userid")]
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

        [Column("dateread")]
        public DateTime? DateRead
        {
            get { return GetProperty<DateTime?>(); }
            set { SetProperty(value); }
        }
    }

}