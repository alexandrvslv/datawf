﻿using DataWF.Data;
using DataWF.Module.Common;
using System;
using System.ComponentModel;
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
        public static readonly DBTable<MessageAddress> DBTable = GetTable<MessageAddress>();
        public static readonly DBColumn UserKey = DBTable.ParseProperty(nameof(UserId));
        public static readonly DBColumn PositionKey = DBTable.ParseProperty(nameof(PositionId));
        public static readonly DBColumn DepartmentKey = DBTable.ParseProperty(nameof(DepartmentId));
        public static readonly DBColumn DateReadKey = DBTable.ParseProperty(nameof(DateRead));

        private User user;
        private Position position;
        private Department department;

        public MessageAddress()
        {
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(table.PrimaryKey); }
            set { SetValue(value, table.PrimaryKey); }
        }

        [Browsable(false)]
        [DataMember, Column("user_id"), Index("dmessage_address_user_id")]
        public int? UserId
        {
            get { return GetValue<int?>(UserKey); }
            set { SetValue(value, UserKey); }
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get { return GetReference(UserKey, ref user); }
            set
            {
                SetReference(user = value, UserKey);
                Position = value?.Position;
            }
        }

        [Browsable(false)]
        [DataMember, Column("position_id", Keys = DBColumnKeys.View), Index("dmessage_address_position_id")]
        public int? PositionId
        {
            get { return GetValue<int?>(PositionKey); }
            set { SetValue(value, PositionKey); }
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get { return GetReference(PositionKey, ref position); }
            set
            {
                SetReference(position = value, PositionKey);
                Department = value?.Department;
            }
        }

        [Browsable(false)]
        [DataMember, Column("department_id", Keys = DBColumnKeys.View), Index("dmessage_address_department_id")]
        public int? DepartmentId
        {
            get { return GetValue<int?>(DepartmentKey); }
            set { SetValue(value, DepartmentKey); }
        }

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get { return GetReference(DepartmentKey, ref department); }
            set { SetReference(department = value, DepartmentKey); }
        }

        [DataMember, Column("date_read")]
        public DateTime? DateRead
        {
            get { return GetValue<DateTime?>(DateReadKey); }
            set { SetValue(value, DateReadKey); }
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