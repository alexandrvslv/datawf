using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Messanger
{

    [DataContract, Table("dmessage_address", "Message", Keys = DBTableKeys.NoLogs), InvokerGenerator]
    public partial class MessageAddress : MessageDetail
    {
        private User user;
        private Position position;
        private Department department;

        public MessageAddress(DBTable table) : base(table)
        {
        }

        public MessageAddressTable<MessageAddress> MessageAddressTable => (MessageAddressTable<MessageAddress>)Table;

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(MessageAddressTable.IdKey);
            set => SetValue(value, MessageAddressTable.IdKey);
        }

        [Browsable(false)]
        [DataMember, Column("user_id"), Index("dmessage_address_user_id")]
        public int? UserId
        {
            get => GetValue<int?>(MessageAddressTable.UserIdKey);
            set => SetValue(value, MessageAddressTable.UserIdKey);
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get => GetReference(MessageAddressTable.UserIdKey, ref user);
            set
            {
                SetReference(user = value, MessageAddressTable.UserIdKey);
                Position = value?.Position;
            }
        }

        [Browsable(false)]
        [DataMember, Column("position_id", Keys = DBColumnKeys.View), Index("dmessage_address_position_id")]
        public int? PositionId
        {
            get => GetValue<int?>(MessageAddressTable.PositionIdKey);
            set => SetValue(value, MessageAddressTable.PositionIdKey);
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get => GetReference(MessageAddressTable.PositionIdKey, ref position);
            set
            {
                SetReference(position = value, MessageAddressTable.PositionIdKey);
                Department = value?.Department;
            }
        }

        [Browsable(false)]
        [DataMember, Column("department_id", Keys = DBColumnKeys.View), Index("dmessage_address_department_id")]
        public int? DepartmentId
        {
            get => GetValue<int?>(MessageAddressTable.DepartmentIdKey);
            set => SetValue(value, MessageAddressTable.DepartmentIdKey);
        }

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get => GetReference(MessageAddressTable.DepartmentIdKey, ref department);
            set => SetReference(department = value, MessageAddressTable.DepartmentIdKey);
        }

        [DataMember, Column("date_read")]
        public DateTime? DateRead
        {
            get => GetValue<DateTime?>(MessageAddressTable.DateReadKey);
            set => SetValue(value, MessageAddressTable.DateReadKey);
        }

        [Browsable(false)]
        public DBItem Staff
        {
            get => (DBItem)User ?? (DBItem)Position ?? (DBItem)Department;
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