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

namespace DataWF.Module.Common
{
    public enum DataLogType
    {
        None,
        Password,
        Authorization,
        Start,
        Stop,
        Execute,
        Insert,
        Update,
        Delete
    }

    [Table("flow", "ddatalog", BlockSize = 2000)]
    public class DataLog : DBItem, ICheck
    {
        public static DBTable<DataLog> DBTable
        {
            get { return DBService.GetTable<DataLog>(); }
        }

        private Dictionary<string, object> cacheRowNew;
        private Dictionary<string, object> cacheRowOld;
        public static DataLog LogStart;
        private byte[] cacheDataOld;
        private byte[] cacheDataNew;
        private DBTable cacheTargetTable;
        private DBItem cacheTarget;
        private DBTable cacheDocumentTable;
        private DBItem cacheDocument;

        public DataLog()
        {
            Build(DBTable);
        }

        public void RefereshText()
        {
            string _textCache = GetProperty<string>(nameof(TextData));
            if (TargetTable != null && _textCache.Length == 0)
            {
                var map = GetLogMapList();
                foreach (var item in map)
                {
                    if (item.Column == null ||
                        //(item.Column.Keys & DBColumnKeys.System) == DBColumnKeys.System ||
                        (item.Column.Keys & DBColumnKeys.Stamp) == DBColumnKeys.Stamp)
                        continue;
                    if (item.Old == DBNull.Value && item.New != DBNull.Value)
                        _textCache += string.Format("{0}: {1}\n", item.Column, item.NewFormat);
                    else if (item.Old != DBNull.Value && item.New == DBNull.Value)
                        _textCache += string.Format("{0}: {1}\n", item.Column, item.OldFormat);
                    else if (item.Old != DBNull.Value && item.New != DBNull.Value)
                    {
                        if (item.Column != null && (item.Column.Keys & DBColumnKeys.Access) == DBColumnKeys.Access)
                        {
                            string rez = string.Empty;

                            AccessValue oldAcces = new AccessValue((byte[])item.Old);
                            AccessValue newAcces = new AccessValue((byte[])item.New);
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
                            _textCache += string.Format("{0}: {1}", item.Column, rez);
                        }
                        else if (item.Column.DataType == typeof(string))
                        {
                            var diffs = DiffResult.DiffLines(item.OldFormat.ToString(), item.NewFormat.ToString());
                            var buf = new StringBuilder();
                            foreach (var diff in diffs)
                            {
                                string val = diff.Result.Trim();
                                if (val.Length > 0)
                                    buf.AppendLine(string.Format("{0} {1} at {2};", diff.Type, val, diff.Index, diff.Index));
                            }
                            _textCache += string.Format("{0}: {1}\r\n", item.Column, buf);
                        }
                        else
                            _textCache += string.Format("{0}: Old:{1} New:{2}\r\n", item.Column, item.OldFormat, item.NewFormat);
                    }
                }
            }
            SetProperty(_textCache, nameof(TextData));
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false)]
        [Column("document_id")]
        public string DocumentId
        {
            get { return GetProperty<string>(nameof(DocumentId)); }
            set { SetProperty(value, nameof(DocumentId)); }
        }

        [Column("document_table")]
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
        [Column("parentid", Keys = DBColumnKeys.Group)]
        public long? ParentId
        {
            get { return GetProperty<long?>(nameof(ParentId)); }
            set { SetProperty(value, nameof(ParentId)); }
        }

        [Reference("fk_ddoclog_parentid", nameof(ParentId))]
        public DataLog Parent
        {
            get { return GetPropertyReference<DataLog>(nameof(ParentId)); }
            set { SetPropertyReference(value, nameof(ParentId)); }
        }

        [Browsable(false)]
        [Column("redoid", Keys = DBColumnKeys.Group)]
        public long? RedoId
        {
            get { return GetProperty<long?>(nameof(RedoId)); }
            set { SetProperty(value, nameof(RedoId)); }
        }

        [Reference("fk_ddoclog_redoid", nameof(RedoId))]
        public DataLog Redo
        {
            get { return GetPropertyReference<DataLog>(nameof(RedoId)); }
            set { SetPropertyReference(value, nameof(RedoId)); }
        }

        [Column("data_new")]
        public byte[] DataNew
        {
            get { return cacheDataNew ?? (cacheDataNew = DBService.GetZip(this, ParseProperty(nameof(DataNew)))); }
            set
            {
                cacheDataNew = value;
                DBService.SetZip(this, ParseProperty(nameof(DataNew)), value);
            }
        }

        [Column("data_old")]
        public byte[] DataOld
        {
            get { return cacheDataOld ?? (cacheDataOld = DBService.GetZip(this, ParseProperty(nameof(DataOld)))); }
            set
            {
                cacheDataOld = value;
                DBService.SetZip(this, ParseProperty(nameof(DataOld)), value);
            }
        }

        public override void OnPropertyChanged(string property, DBColumn column = null, object value = null)
        {
            base.OnPropertyChanged(property, column, value);
        }

        [Column("text_data", ColumnType = DBColumnTypes.Internal)]
        public string TextData
        {
            get { return GetProperty<string>(nameof(TextData)); }
            set { SetProperty(value, nameof(TextData)); }
        }

        [Column("target_table")]
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

        [Column("targetid")]
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
            }
        }

        [Column("typeid", Keys = DBColumnKeys.Type)]
        public DataLogType? LogType
        {
            get { return GetProperty<DataLogType?>(nameof(LogType)); }
            set { SetProperty(value, nameof(LogType)); }
        }

        public Dictionary<string, object> NewMap
        {
            get
            {
                if (cacheRowNew != null || DataNew == null)
                    return cacheRowNew;

                cacheRowNew = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
                try { DBItemBinarySerialize.ReadMap(DataNew, TargetTable, cacheRowNew); }
                catch (Exception ex) { Helper.OnException(ex); }
                return cacheRowNew;
            }
        }

        public Dictionary<string, object> OldMap
        {
            get
            {
                if (cacheRowOld != null || DataOld == null)
                    return cacheRowOld;
                cacheRowOld = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
                try { DBItemBinarySerialize.ReadMap(DataOld, TargetTable, cacheRowOld); }
                catch (Exception ex) { Helper.OnException(ex); }
                return cacheRowOld;
            }
        }

        [Column("userid")]
        public int? UserId
        {
            get { return GetProperty<int?>(nameof(UserId)); }
            set { SetProperty(value, nameof(UserId)); }
        }

        [Reference("fk_ddoclog_userid", nameof(UserId))]
        public User User
        {
            get { return GetPropertyReference<User>(nameof(UserId)); }
            set { SetPropertyReference(value, nameof(UserId)); }
        }

        public SelectableList<LogChange> GetLogMapList()
        {
            DBTable table = TargetTable;
            SelectableList<LogChange> listmap = new SelectableList<LogChange>();
            Dictionary<string, object> vals = NewMap == null ? OldMap : NewMap;
            if (vals != null)
            {
                foreach (KeyValuePair<string, object> item in vals)
                {
                    LogChange map = new LogChange();
                    map.Column = table.ParseColumn(item.Key);
                    map.Old = OldMap != null && OldMap.ContainsKey(item.Key) ? OldMap[item.Key] : null;
                    map.New = NewMap != null && NewMap.ContainsKey(item.Key) ? NewMap[item.Key] : null;
                    listmap.Add(map);
                }
            }
            return listmap;
        }

        [ThreadStatic]
        public static DBItem CurrentDocument;

        public static void LogUser(User user, DataLogType type, string info, DBItem item = null)
        {
            var newLog = new DataLog();
            newLog.User = user;
            newLog.LogType = type;
            newLog.Parent = LogStart;
            newLog.TargetItem = item;
            newLog.Document = CurrentDocument;

            string text = info;
            if (type == DataLogType.Start)
            {
                if (LogStart == null || LogStart.User != user)
                    LogStart = newLog;
                else
                    return;

                var prop = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();

                string address = string.Empty;
                foreach (var ip in System.Net.Dns.GetHostAddresses(prop.HostName))
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        address += ip.ToString() + "; ";
                text = string.Format("{0} on {1}-{2}({3})", info, prop.DomainName, prop.HostName, address);
            }

            newLog.TextData = text;
            newLog.Save(false);
        }

        public static DataLog LogRow(DataLog parent, DBItem row, List<DBColumn> cols, DBUpdateState state = DBUpdateState.Default, DBStatus status = DBStatus.New)
        {
            var log = new DataLog();
            if (state == DBUpdateState.Default)
                state = row.DBState;

            if (state == DBUpdateState.Insert)
            {
                log.LogType = DataLogType.Insert;
                if (!DBService.Schems.Contains(row.Table.Schema))
                {
                    log.TextData = "Export";
                    status = DBStatus.Actual;
                }
                else
                    log.DataNew = DBItemBinarySerialize.Write(row, cols);
            }
            else if ((state & DBUpdateState.Delete) == DBUpdateState.Delete)
            {
                log.LogType = DataLogType.Delete;
                log.DataOld = DBItemBinarySerialize.Write(row);
            }
            else if ((state & DBUpdateState.Update) == DBUpdateState.Update)
            {
                log.LogType = DataLogType.Update;
                log.DataNew = DBItemBinarySerialize.Write(row, cols);
                log.DataOld = DBItemBinarySerialize.Write(row, cols, true);
            }

            log.Status = status;
            log.TargetId = row.PrimaryId.ToString();
            log.TargetTable = row.Table;
            log.User = User.CurrentUser;
            log.Parent = parent;
            log.Document = CurrentDocument;

            return log;
        }

        public static void Reject(IList<DataLog> redo)
        {
            ListHelper.QuickSort<DataLog>(redo, new DBComparer(DBTable.PrimaryKey, ListSortDirection.Descending));
            var changed = new Dictionary<DBItem, List<DataLog>>();
            foreach (DataLog log in redo)
            {
                DBItem row = log.TargetItem;
                if (row == null)
                {
                    if (log.LogType == DataLogType.Insert)
                        continue;
                    row = log.TargetTable.NewItem(DBUpdateState.Insert, false);
                    row.SetValue(DBService.ParseValue(log.TargetTable.PrimaryKey, log.TargetId), log.TargetTable.PrimaryKey, false);
                }
                else if (log.LogType == DataLogType.Delete && !changed.ContainsKey(row))
                    continue;

                Dictionary<string, object> temp = log.OldMap;

                if (temp != null)
                {
                    foreach (KeyValuePair<string, object> kvp in temp)
                        row.SetValue(kvp.Value, log.TargetTable.Columns[kvp.Key], log.LogType == DataLogType.Update);
                }

                if (log.LogType == DataLogType.Insert)
                {
                    row.DBState |= DBUpdateState.Delete;
                }
                else if (log.LogType == DataLogType.Delete)
                {
                    row.DBState |= DBUpdateState.Insert;
                    log.TargetTable.Add(row);
                }
                else if (log.LogType == DataLogType.Update && row.GetIsChanged())
                {
                    row.DBState |= DBUpdateState.Update;
                }

                if (!changed.ContainsKey(row))
                    changed.Add(row, new List<DataLog>());

                log.Status = DBStatus.Delete;
                changed[row].Add(log);
            }
            foreach (var row in changed)
            {
                var log = DataLog.LogRow(LogStart, row.Key, row.Key.GetChangeKeys().ToList(), row.Key.DBState, (row.Key.DBState & DBUpdateState.Insert) == DBUpdateState.Insert ? DBStatus.New : DBStatus.Edit);
                log.TextData = "Reject";
                log.Save(false);

                foreach (var item in row.Value)
                {
                    item.Redo = log;
                    item.Save();
                }
                var loging = row.Key.Table.IsLoging;
                row.Key.Table.IsLoging = false;
                row.Key.Save();
                row.Key.Table.IsLoging = loging;
            }
        }

        public static DataLog Accept(DBItem row, IList<DataLog> logs)
        {
            if (row.Status == DBStatus.Edit || row.Status == DBStatus.New || row.Status == DBStatus.Error)
                row.Status = DBStatus.Actual;
            else if (row.Status == DBStatus.Delete)
                row.Delete();

            var log = DataLog.LogRow(LogStart, row, row.GetChangeKeys().ToList(), row.DBState, DBStatus.Actual);
            log.TextData = "Accept";
            log.Save(false);

            var loging = row.Table.IsLoging;
            row.Table.IsLoging = false;
            row.Save();
            row.Table.IsLoging = loging;

            foreach (DataLog item in logs)
                if (item.Status == DBStatus.New)
                {
                    item.Redo = log;
                    item.Status = DBStatus.Actual;
                    item.Save();
                }
            return log;
        }
    }
}
