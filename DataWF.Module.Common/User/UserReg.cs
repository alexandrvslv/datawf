using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{

    [Table("duser_log", "User", BlockSize = 500, Keys = DBTableKeys.NoLogs)]
    public sealed partial class UserReg : DBUserReg
    {

        private UserReg redo;

        [Reference(nameof(UserId))]
        [Column("type_id", Keys = DBColumnKeys.ElementType | DBColumnKeys.View)]
        public UserRegType? RegType
        {
            get => GetValue(Table.RegTypeKey);
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
            get => GetValue(Table.RedoIdKey);
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
            get => GetValue(Table.TextDataKey);
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
