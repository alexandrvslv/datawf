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

    [DataContract, Table("dmessage", "Message", Keys = DBTableKeys.NoLogs)]
    public sealed partial class Message : DBItem, IDisposable
    {

        private MessageAddressList addresses;
        private User user;

        public Message(DBTable table) : base(table)
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get => GetValue<long?>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Browsable(false), Column("user_id", Keys = DBColumnKeys.View)]
        public int? UserId
        {
            get => GetValue<int?>(Table.UserIdKey);
            set => SetValue(value, Table.UserIdKey);
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get => GetReference(Table.UserIdKey, ref user);
            set => SetReference(user = value, Table.UserIdKey);
        }

        [Column("text_data")]
        public string Data
        {
            get => GetValue<string>(Table.DataKey);
            set => SetValue(value, Table.DataKey);
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

