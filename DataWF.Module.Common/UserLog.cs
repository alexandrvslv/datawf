/*
 DocumentLog.cs

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
using System;
using System.Collections.Generic;
using System.Text;
using DataWF.Data;
using DataWF.Common;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Linq;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{
    public enum UserLogType
    {
        None,
        Password,
        Authorization,
        Start,
        Stop,
        Execute,
        Insert,
        Update,
        Delete,
        Reject
    }

    [DataContract, Table("wf_common", "duser_log", "User", BlockSize = 2000, IsLoging = false)]
    public class UserLog : DBItem, ICheck
    {
        [ThreadStatic]
        public static DBItem CurrentDocument;
        [ThreadStatic]
        public static UserLog CurrentLog;

        public static DBTable<UserLog> DBTable
        {
            get { return DBService.GetTable<UserLog>(); }
        }

        public static event EventHandler<DBItemEventArgs> RowLoging;

        public static event EventHandler<DBItemEventArgs> RowLoged;

        public static void OnDBRowUpdate(DBItemEventArgs arg)
        {
            if (arg.Item.Table == UserLog.DBTable
                || arg.Item.Table is DBLogTable)
                return;
            RowLoging?.Invoke(null, arg);
            var log = UserLog.LogRow(arg);
            //if (transaction.DbConnection != DBTable.Schema.Connection)
            if (arg.Transaction.SubTransaction == null)
            {
                arg.Transaction.BeginSubTransaction(UserLog.DBTable.Schema);
                arg.Transaction.SubTransaction.Reference = false;
            }
            log.Save(arg.Transaction.SubTransaction);
            RowLoged?.Invoke(log, arg);
        }

        private DBLogItem cacheLogItem;
        private DBTable cacheTargetTable;
        private DBItem cacheTarget;
        private DBTable cacheDocumentTable;
        private DBItem cacheDocument;

        public UserLog()
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("user_id", Keys = DBColumnKeys.View)]
        public int? UserId
        {
            get { return GetProperty<int?>(nameof(UserId)); }
            set { SetProperty(value, nameof(UserId)); }
        }

        [Reference("fk_duser_log_user_id", nameof(UserId))]
        public User User
        {
            get { return GetPropertyReference<User>(nameof(UserId)); }
            set { SetPropertyReference(value, nameof(UserId)); }
        }

        [DataMember, Column("type_id", Keys = DBColumnKeys.ElementType | DBColumnKeys.View)]
        public UserLogType? LogType
        {
            get { return GetProperty<UserLogType?>(nameof(LogType)); }
            set { SetProperty(value, nameof(LogType)); }
        }

        [DataMember, Column("document_table", 512)]
        public string DocumentTableName
        {
            get { return GetProperty<string>(nameof(DocumentTableName)); }
            set { SetProperty(value, nameof(DocumentTableName)); }
        }

        [XmlIgnore]
        public DBTable DocumentTable
        {
            get { return cacheDocumentTable ?? (cacheDocumentTable = DBService.ParseTable(DocumentTableName)); }
            set
            {
                cacheDocumentTable = value;
                DocumentTableName = value?.Name;
            }
        }

        [Browsable(false)]
        [DataMember, Column("document_id", 256)]
        public string DocumentId
        {
            get { return GetProperty<string>(nameof(DocumentId)); }
            set { SetProperty(value, nameof(DocumentId)); }
        }

        [XmlIgnore]
        public DBItem Document
        {
            get { return cacheDocument ?? (cacheDocument = (DocumentTable?.LoadItemById(DocumentId) ?? DBItem.EmptyItem)); }
            set
            {
                cacheDocument = value;
                DocumentId = value?.PrimaryId.ToString();
                DocumentTable = value?.Table;
            }
        }

        [Browsable(false)]
        [DataMember, Column("parent_id", Keys = DBColumnKeys.Group)]
        public long? ParentId
        {
            get { return GetProperty<long?>(nameof(ParentId)); }
            set { SetProperty(value, nameof(ParentId)); }
        }

        [Reference("fk_duser_log_parent_id", nameof(ParentId))]
        public UserLog Parent
        {
            get { return GetPropertyReference<UserLog>(nameof(ParentId)); }
            set { SetPropertyReference(value, nameof(ParentId)); }
        }

        [Browsable(false)]
        [DataMember, Column("redo_id", Keys = DBColumnKeys.Group)]
        public long? RedoId
        {
            get { return GetProperty<long?>(nameof(RedoId)); }
            set { SetProperty(value, nameof(RedoId)); }
        }

        [Reference("fk_duser_log_redo_id", nameof(RedoId))]
        public UserLog Redo
        {
            get { return GetPropertyReference<UserLog>(nameof(RedoId)); }
            set { SetPropertyReference(value, nameof(RedoId)); }
        }

        [DataMember, Column("target_table", 512)]
        public string TargetTableName
        {
            get { return GetProperty<string>(nameof(TargetTableName)); }
            set { SetProperty(value, nameof(TargetTableName)); }
        }

        [XmlIgnore]
        public DBTable TargetTable
        {
            get { return cacheTargetTable ?? (cacheTargetTable = DBService.ParseTable(TargetTableName, DBService.DefaultSchema)); }
            set
            {
                cacheTargetTable = value;
                TargetTableName = value?.FullName;
            }
        }

        [DataMember, Column("target_id", 256)]
        public string TargetId
        {
            get { return GetProperty<string>(nameof(TargetId)); }
            set { SetProperty(value, nameof(TargetId)); }
        }

        [XmlIgnore]
        public DBItem TargetItem
        {
            get { return cacheTarget ?? (cacheTarget = (TargetTable?.LoadItemById(TargetId) ?? DBItem.EmptyItem)); }
            set
            {
                cacheTarget = value;
                TargetId = value?.PrimaryId.ToString();
                TargetTable = value?.Table;

                if (value == null)
                    return;
                if (value.UpdateState.HasFlag(DBUpdateState.Insert))
                {
                    LogType = UserLogType.Insert;
                }
                else if (value.UpdateState.HasFlag(DBUpdateState.Delete))
                {
                    LogType = UserLogType.Delete;
                }
                else if (value.UpdateState.HasFlag(DBUpdateState.Update))
                {
                    LogType = UserLogType.Update;
                }
            }
        }

        public DBLogTable LogTable
        {
            get { return TargetTable?.LogTable; }
        }

        [DataMember, Column("log_id")]
        public int? LogId
        {
            get { return GetProperty<int?>(nameof(LogId)); }
            set { SetProperty(value, nameof(LogId)); }
        }

        public DBLogItem LogItem
        {
            get { return cacheLogItem ?? (cacheLogItem = LogTable?.LoadById(LogId)); }
            set
            {
                cacheLogItem = value;
                if (value != null)
                {
                    LogId = value.LogId;
                    TargetItem = value.BaseItem;
                }
            }
        }

        [DataMember, Column("text_data", ColumnType = DBColumnTypes.Internal)]
        public string TextData
        {
            get { return GetProperty<string>(nameof(TextData)); }
            set { SetProperty(value, nameof(TextData)); }
        }

        public override void OnPropertyChanged(string property, DBColumn column = null, object value = null)
        {
            base.OnPropertyChanged(property, column, value);
        }

        public void RefereshText()
        {
            string _textCache = GetProperty<string>(nameof(TextData));
            if (_textCache.Length == 0 && LogItem != null)
            {
                var logPrevius = LogItem.GetPrevius();
                foreach (var logColumn in LogTable.GetLogColumns())
                {
                    var column = logColumn.BaseColumn;
                    if ((column.Keys & DBColumnKeys.Stamp) == DBColumnKeys.Stamp)
                        continue;
                    var oldValue = logPrevius?.GetValue(logColumn);
                    var newValue = LogItem.GetValue(logColumn);
                    if (DBService.Equal(oldValue, newValue))
                        continue;
                    var oldFormat = column.Access.View ? column.FormatValue(oldValue) : "*****";
                    var newFormat = column.Access.View ? column.FormatValue(newValue) : "*****";
                    if (oldValue == null && newValue != null)
                        _textCache += string.Format("{0}: {1}\n", column, newFormat);
                    else if (oldValue != null && newValue == null)
                        _textCache += string.Format("{0}: {1}\n", column, oldFormat);
                    else if (oldValue != null && newValue != null)
                    {
                        if ((column.Keys & DBColumnKeys.Access) == DBColumnKeys.Access)
                        {
                            string rez = string.Empty;
                            AccessValue oldAcces = new AccessValue((byte[])oldValue);
                            AccessValue newAcces = new AccessValue((byte[])newValue);
                            foreach (var oAccess in oldAcces.Items)
                            {
                                int index = newAcces.GetIndex(oAccess.Group);
                                if (index < 0)
                                    rez += string.Format("Remove {0}; ", oAccess);
                                else if (!newAcces.Items[index].Equals(oAccess))
                                {
                                    var nAceess = newAcces.Items[index];
                                    rez += string.Format("Change {0}({1}{2}{3}{4}{5}{6}); ", oAccess.Group,
                                        oAccess.View != nAceess.View ? (nAceess.View ? "+" : "-") + "View " : "",
                                        oAccess.Edit != nAceess.Edit ? (nAceess.Edit ? "+" : "-") + "Edit " : "",
                                        oAccess.Create != nAceess.Create ? (nAceess.Create ? "+" : "-") + "Create " : "",
                                        oAccess.Delete != nAceess.Delete ? (nAceess.Delete ? "+" : "-") + "Delete " : "",
                                        oAccess.Admin != nAceess.Admin ? (nAceess.Admin ? "+" : "-") + "Admin " : "",
                                        oAccess.Accept != nAceess.Accept ? (nAceess.Accept ? "+" : "-") + "Accept" : "");
                                }
                            }
                            foreach (var nAccess in newAcces.Items)
                            {
                                int index = oldAcces.GetIndex(nAccess.Group);
                                if (index < 0)
                                    rez += string.Format("New {0}; ", nAccess);
                            }
                            _textCache += string.Format("{0}: {1}", column, rez);
                        }
                        else if (column.DataType == typeof(string))
                        {
                            var diffs = DiffResult.DiffLines(oldFormat, newFormat);
                            var buf = new StringBuilder();
                            foreach (var diff in diffs)
                            {
                                string val = diff.Result.Trim();
                                if (val.Length > 0)
                                    buf.AppendLine(string.Format("{0} {1} at {2};", diff.Type, val, diff.Index, diff.Index));
                            }
                            _textCache += string.Format("{0}: {1}\r\n", column, buf);
                        }
                        else
                            _textCache += string.Format("{0}: Old:{1} New:{2}\r\n", column, oldFormat, newFormat);
                    }
                }
            }
            SetProperty(_textCache, nameof(TextData));
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

        public static void LogUser(User user, UserLogType type, string info, DBItem item = null)
        {
            var newLog = new UserLog()
            {
                User = user,
                LogType = type,
                Parent = user.LogStart,
                TargetItem = item,
                Document = CurrentDocument
            };

            string text = info;
            if (type == UserLogType.Authorization)
            {
                user.LogStart = newLog;
                var prop = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();

                string address = string.Empty;
                foreach (var ip in System.Net.Dns.GetHostAddresses(prop.HostName))
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        address += ip + "; ";
                text = string.Format("{0} on {1}-{2}({3})", info, prop.DomainName, prop.HostName, address);
            }

            newLog.TextData = text;
            newLog.Save(false);
        }

        public static UserLog LogRow(DBItemEventArgs arg)
        {
            var parent = CurrentLog ?? arg.Transaction.Tag as UserLog ?? User.CurrentUser.LogStart;
            var log = new UserLog()
            {
                User = User.CurrentUser,
                Parent = parent,
                Document = CurrentDocument
            };
            if (arg.LogItem != null)
            {
                log.LogItem = arg.LogItem;
            }
            else
            {
                log.TargetItem = arg.Item;
            }
            return log;
        }

        public static void Reject(IList<UserLog> redo)
        {
            ListHelper.QuickSort<UserLog>(redo, new DBComparer(DBTable.PrimaryKey, ListSortDirection.Descending));
            var changed = new Dictionary<DBItem, List<UserLog>>();
            foreach (UserLog log in redo)
            {
                DBItem row = log.TargetItem;
                if (row == null)
                {
                    if (log.LogType == UserLogType.Insert)
                        continue;
                    row = log.TargetTable.NewItem(DBUpdateState.Insert, false);
                    row.SetValue(log.TargetTable.PrimaryKey.ParseValue(log.TargetId), log.TargetTable.PrimaryKey, false);
                }
                else if (log.LogType == UserLogType.Delete && !changed.ContainsKey(row))
                    continue;

                if (log.LogItem != null)
                {
                    log.LogItem.Upload(row);
                }

                if (log.LogType == UserLogType.Insert)
                {
                    row.UpdateState |= DBUpdateState.Delete;
                }
                else if (log.LogType == UserLogType.Delete)
                {
                    row.UpdateState |= DBUpdateState.Insert;
                    log.TargetTable.Add(row);
                }
                else if (log.LogType == UserLogType.Update && row.GetIsChanged())
                {
                    row.UpdateState |= DBUpdateState.Update;
                }

                log.Status = DBStatus.Delete;

                if (!changed.TryGetValue(row, out var list))
                    changed[row] = list = new List<UserLog>();

                list.Add(log);
            }
            foreach (var entry in changed)
            {
                CurrentLog = new UserLog() { TextData = "Reject" };
                entry.Key.Save();

                foreach (var item in entry.Value)
                {
                    item.Redo = CurrentLog;
                    item.Save();
                }

            }
            CurrentLog = null;
        }

        public static UserLog Accept(DBItem row, IList<UserLog> logs)
        {
            if (row.Status == DBStatus.Edit || row.Status == DBStatus.New || row.Status == DBStatus.Error)
                row.Status = DBStatus.Actual;
            else if (row.Status == DBStatus.Delete)
                row.Delete();

            var log = CurrentLog = new UserLog { TextData = "Accept" };
            row.Save();

            foreach (UserLog item in logs)
            {
                if (item.Status == DBStatus.New)
                {
                    item.Redo = log;
                    item.Status = DBStatus.Actual;
                    item.Save();
                }
            }
            CurrentLog = null;

            return log;
        }
    }
}
