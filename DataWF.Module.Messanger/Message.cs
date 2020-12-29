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

    [DataContract, Table("dmessage", "Message", Keys = DBTableKeys.NoLogs), InvokerGenerator]
    public sealed partial class Message : DBItem, IDisposable
    {

        private MessageAddressList addresses;
        private User user;

        public Message(DBTable table) : base(table)
        {
        }

        public MessageTable MessageTable => (MessageTable)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get => GetValue<long?>(MessageTable.IdKey);
            set => SetValue(value, MessageTable.IdKey);
        }

        [Browsable(false), Column("user_id", Keys = DBColumnKeys.View)]
        public int? UserId
        {
            get => GetValue<int?>(MessageTable.UserIdKey);
            set => SetValue(value, MessageTable.UserIdKey);
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get => GetReference(MessageTable.UserIdKey, ref user);
            set => SetReference(user = value, MessageTable.UserIdKey);
        }

        [Column("text_data")]
        public string Data
        {
            get => GetValue<string>(MessageTable.DataKey);
            set => SetValue(value, MessageTable.DataKey);
        }

        public MessageAddressList Addresses
        {
            get
            {
                if (addresses == null && Id != null)
                    addresses = new MessageAddressList((MessageAddressTable<MessageAddress>)Schema.GetTable<MessageAddress>(), this);
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

