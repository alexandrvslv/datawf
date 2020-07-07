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
        public static readonly DBTable<Message> DBTable = GetTable<Message>();
        public static readonly DBColumn UserKey = DBTable.ParseProperty(nameof(User));
        public static readonly DBColumn DataKey = DBTable.ParseProperty(nameof(Data));

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
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false), Column("user_id", Keys = DBColumnKeys.View)]
        public int? UserId
        {
            get { return GetValue<int?>(UserKey); }
            set { SetValue(value, UserKey); }
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get { return GetReference(UserKey, ref user); }
            set { SetReference(user = value, UserKey); }
        }

        [Column("text_data")]
        public string Data
        {
            get { return GetValue<string>(DataKey); }
            set { SetValue(value, DataKey); }
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

