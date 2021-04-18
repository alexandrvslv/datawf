using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Messanger
{

    [DataContract, Table("dmessage_address", "Message", Keys = DBTableKeys.NoLogs)]
    public partial class MessageAddress : MessageDetail
    {
        private User user;
        private Position position;
        private Department department;

        public MessageAddress(DBTable table) : base(table)
        {
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Browsable(false)]
        [DataMember, Column("user_id"), Index("dmessage_address_user_id")]
        public int? UserId
        {
            get => GetValue<int?>(Table.UserIdKey);
            set => SetValue(value, Table.UserIdKey);
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get => GetReference(Table.UserIdKey, ref user);
            set
            {
                SetReference(user = value, Table.UserIdKey);
                Position = value?.Position;
            }
        }

        [Browsable(false)]
        [DataMember, Column("position_id", Keys = DBColumnKeys.View), Index("dmessage_address_position_id")]
        public int? PositionId
        {
            get => GetValue<int?>(Table.PositionIdKey);
            set => SetValue(value, Table.PositionIdKey);
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get => GetReference(Table.PositionIdKey, ref position);
            set
            {
                SetReference(position = value, Table.PositionIdKey);
                Department = value?.Department;
            }
        }

        [Browsable(false)]
        [DataMember, Column("department_id", Keys = DBColumnKeys.View), Index("dmessage_address_department_id")]
        public int? DepartmentId
        {
            get => GetValue<int?>(Table.DepartmentIdKey);
            set => SetValue(value, Table.DepartmentIdKey);
        }

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get => GetReference(Table.DepartmentIdKey, ref department);
            set => SetReference(department = value, Table.DepartmentIdKey);
        }

        [DataMember, Column("date_read")]
        public DateTime? DateRead
        {
            get => GetValue<DateTime?>(Table.DateReadKey);
            set => SetValue(value, Table.DateReadKey);
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