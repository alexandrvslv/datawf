using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataWF.Module.Messanger
{
    public partial class MessageTable
    {
        [ControllerMethod]
        public Task<Message> SendToGroup(User from, UserGroup group, string data)
        {
            return Send(from, group.GetUsers(), data);
        }

        [ControllerMethod]
        public Task<Message> SendToUser(User from, User to, string data)
        {
            return Send(from, new[] { to }, data);
        }

        [ControllerMethod]
        public Task<Message> SendToPosition(User from, Position to, string data)
        {
            return Send(from, new[] { to }, data);
        }

        [ControllerMethod]
        public Task<Message> SendToDepartment(User from, Department to, string data)
        {
            return Send(from, new[] { to }, data);
        }

        public async Task<Message> Send(User from, IEnumerable<DBItem> to, string data)
        {
            using (var transaction = new DBTransaction(this, from))
            {
                try
                {
                    var message = new Message(this)
                    {
                        DateCreate = DateTime.UtcNow,
                        User = from,
                        Data = data
                    };
                    await message.Save(transaction);

                    foreach (var staff in to)
                    {
                        if (staff != message.User)//&& user.Status == DBStatus.Actual
                        {
                            var address = new MessageAddress(Schema.GetTable<MessageAddress>())
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
    }
}

