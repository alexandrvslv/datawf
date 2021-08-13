using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{
    public enum UserRegType
    {
        None,
        Password,
        Authorization,
        Start,
        Stop,
        Execute,
        Transaction,
        Reject
    }

    public enum UserRegStrategy
    {
        ByItem,
        ByTransaction,
        BySession
    }

    [Table("duser_log", "User", BlockSize = 500, Keys = DBTableKeys.NoLogs)]
    public sealed partial class UserReg : DBUserReg
    {

        private User user;
        private UserReg redo;

        [Reference(nameof(UserId))]
        public User User
        {
            get => GetReference(Table.UserIdKey, ref user);
            set => SetReference(user = value, Table.UserIdKey);
        }

        [Column("type_id", Keys = DBColumnKeys.ElementType | DBColumnKeys.View)]
        public UserRegType? RegType
        {
            get => GetValue<UserRegType?>(Table.RegTypeKey);
            set => SetValue(value, Table.RegTypeKey);
        }

        [Browsable(false)]
        [Column("parent_id", Keys = DBColumnKeys.Group)]
        public long? ParentId
        {
            get => GetGroupValue<long?>();
            set => SetGroupValue(value);
        }

        [Reference(nameof(ParentId))]
        public UserReg Parent
        {
            get => GetGroupReference<UserReg>();
            set => SetGroupReference(value);
        }

        [Browsable(false)]
        [Column("redo_id")]
        public long? RedoId
        {
            get => GetValue<long?>(Table.RedoIdKey);
            set => SetValue(value, Table.RedoIdKey);
        }

        [Reference(nameof(RedoId))]
        public UserReg Redo
        {
            get => GetReference(Table.RedoIdKey, ref redo);
            set => SetReference(redo = value, Table.RedoIdKey);
        }

        [Column("text_data")]
        public string TextData
        {
            get => GetValue<string>(Table.TextDataKey);
            set => SetValue(value, Table.TextDataKey);
        }

        public List<UserRegItem> Items { get; set; }

        protected override void RaisePropertyChanged(string property)
        {
            base.RaisePropertyChanged(property);
        }

        //public SelectableList<LogChange> GetLogMapList()
        //{
        //    DBTable table = TargetTable;
        //    SelectableList<LogChange> listmap = new SelectableList<LogChange>();
        //    if (LogItem != null)
        //    {
        //        foreach (var column in TargetTable.Columns)
        //        {
        //            var logColumn = LogTable.GetLogColumn(column);
        //            if (logColumn != null)
        //            {
        //                LogChange field = new LogChange();
        //                field.Column = column;
        //                field.Old = OldLogItem?.GetValue(logColumn);
        //                field.New = LogItem?.GetValue(logColumn);
        //                listmap.Add(field);
        //            }
        //        }
        //    }
        //    return listmap;
        //}

      


    }
}
