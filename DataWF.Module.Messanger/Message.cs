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
using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace DataWF.Module.Messanger
{
    public class MessageList : DBTableView<Message>
    {
        public MessageList(string filter)
            : base(Message.DBTable, filter)
        {
            //_ApplySort(new DBRowComparer(FlowEnvir.Config.StageParameter.Table, FlowEnvir.Config.StageParameter.Table.PrimaryKey.Code, ListSortDirection.Ascending));
        }

        public MessageList()
            : this(string.Empty)
        { }

        public MessageList(User fromUser, User user)
            : this(string.Format("({1} = {2} and {0} in (select {3} from {4} where {5} = {6})) or ({1} = {6} and {0} in(select {3} from {4} where {5} = {2}))",
                                 Message.DBTable.PrimaryKey.Name,
                                 Message.DBTable.ParseProperty(nameof(Message.UserId)).Name,
                                 user.Id,
                                 MessageAddress.DBTable.ParseProperty(nameof(MessageAddress.MessageId)).Name,
                                 MessageAddress.DBTable.Name,
                                 MessageAddress.DBTable.ParseProperty(nameof(MessageAddress.UserId)).Name,
                                 fromUser.Id))
        { }
    }

    [DataContract, Table("dmessage", "Message", IsLoging = false)]
    public class Message : DBItem, IDisposable
    {
        public static DBTable<Message> DBTable
        {
            get { return GetTable<Message>(); }
        }

        [ControllerMethod]
        public static Task<Message> SendToGroup(User from, UserGroup group, string data)
        {
            return Send(from, group.GetUsers(), data);
        }

        [ControllerMethod]
        public static Task<Message> SendToUser(User from, User to, string data)
        {
            return Send(from, new[] { to }, data);
        }

        [ControllerMethod]
        public static Task<Message> SendToPosition(User from, Position to, string data)
        {
            return Send(from, new[] { to }, data);
        }

        [ControllerMethod]
        public static Task<Message> SendToDepartment(User from, Department to, string data)
        {
            return Send(from, new[] { to }, data);
        }

        public static async Task<Message> Send(User from, IEnumerable<DBItem> to, string data)
        {
            using (var transaction = new DBTransaction(Message.DBTable.Connection, from))
            {
                try
                {
                    var message = new Message()
                    {
                        DateCreate = DateTime.Now,
                        User = from,
                        Data = data
                    };
                    await message.Save(transaction);

                    foreach (var staff in to)
                    {
                        if (staff != message.User)//&& user.Status == DBStatus.Actual
                        {
                            var address = new MessageAddress
                            {
                                Message = message,
                                Staff = staff
                            };
                            await address.Save(transaction);
                        }
                    }
                    transaction.Commit();
                    return message;
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    transaction.Rollback();
                    throw ex;
                }
            }
        }
        private MessageAddressList addresses;
        private User user;

        public Message()
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetProperty<long?>(); }
            set { SetProperty(value); }
        }

        [Browsable(false), Column("user_id", Keys = DBColumnKeys.View)]
        public int? UserId
        {
            get { return GetProperty<int?>(nameof(UserId)); }
            set { SetProperty(value, nameof(UserId)); }
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get { return GetPropertyReference(ref user); }
            set { user = SetPropertyReference(value); }
        }

        [Column("text_data")]
        public string Data
        {
            get { return GetProperty<string>(nameof(Data)); }
            set { SetProperty(value, nameof(Data)); }
        }

        public MessageAddressList Addresses
        {
            get
            {
                if (addresses == null && Id != null)
                    addresses = new MessageAddressList(this);
                return addresses;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            addresses?.Dispose();
        }
    }
}

