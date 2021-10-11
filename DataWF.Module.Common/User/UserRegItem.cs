using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;

namespace DataWF.Module.Common
{
    [InvokerGenerator]
    public partial class UserRegItem
    {
        private DBItemLog cacheLogItem;
        private IDBTable cacheTargetTable;
        private DBItem cacheTarget;

        [Browsable(false)]
        public string TableName { get; set; }

        [XmlIgnore]
        public DBProvider Provider => DBProvider.Default;

        [XmlIgnore]
        public IDBTable Table
        {
            get => cacheTargetTable ?? (cacheTargetTable = Provider.ParseTable(TableName));
            set
            {
                cacheTargetTable = value;
                TableName = value?.FullName;
            }
        }

        [Browsable(false)]
        public string ItemId { get; set; }

        [XmlIgnore]
        public DBItem Item
        {
            get => cacheTarget ?? (cacheTarget = (Table?.LoadById<DBItem>(ItemId) ?? DBItem.EmptyItem));
            set
            {
                cacheTarget = value;
                ItemId = value?.PrimaryId.ToString();
                Table = value?.Table;
            }
        }

        public IDBTableLog LogTable
        {
            get { return Table?.LogTable; }
        }

        [Browsable(false)]
        public long? LogId { get; set; }

        [XmlIgnore]
        public DBItemLog LogItem
        {
            get => cacheLogItem ?? (cacheLogItem = LogTable?.LoadById<DBItemLog>(LogId));
            set
            {
                cacheLogItem = value;
                if (value != null)
                {
                    LogId = value.LogId;
                    Item = value.BaseItem;
                }
            }
        }

        public void RefereshText(IUserIdentity user)
        {
            string _textCache = "";
            if (_textCache?.Length == 0 && LogItem != null)
            {
                var logPrevius = LogItem.GetPrevius();
                foreach (var logColumn in LogTable.GetLogColumns())
                {
                    var column = logColumn.TargetColumn;
                    if ((column.Keys & DBColumnKeys.Stamp) == DBColumnKeys.Stamp)
                        continue;
                    var oldValue = logPrevius?.GetValue(logColumn);
                    var newValue = LogItem.GetValue(logColumn);
                    if (DBService.Equal(oldValue, newValue))
                        continue;
                    var oldFormat = column.Access.GetFlag(AccessType.Read, user) ? column.FormatDisplay(oldValue) : "*****";
                    var newFormat = column.Access.GetFlag(AccessType.Read, user) ? column.FormatDisplay(newValue) : "*****";
                    if (oldValue == null && newValue != null)
                        _textCache += string.Format("{0}: {1}\n", column, newFormat);
                    else if (oldValue != null && newValue == null)
                        _textCache += string.Format("{0}: {1}\n", column, oldFormat);
                    else if (oldValue != null && newValue != null)
                    {
                        if ((column.Keys & DBColumnKeys.Access) == DBColumnKeys.Access)
                        {
                            string rez = string.Empty;
                            var oldAcces = new AccessValue((byte[])oldValue, Provider);
                            var newAcces = new AccessValue((byte[])newValue, Provider);
                            foreach (var oAccess in oldAcces.Items)
                            {
                                var nAceess = newAcces.Get(oAccess.Identity);
                                if (nAceess.Equals(AccessItem.Empty))
                                    rez += string.Format("Remove {0}; ", oAccess);
                                else if (!nAceess.Equals(oAccess))
                                {
                                    rez += string.Format("Change {0}({1}{2}{3}{4}{5}{6}); ", oAccess.Identity,
                                        oAccess.Read != nAceess.Read ? (nAceess.Read ? "+" : "-") + "View " : "",
                                        oAccess.Update != nAceess.Update ? (nAceess.Update ? "+" : "-") + "Edit " : "",
                                        oAccess.Create != nAceess.Create ? (nAceess.Create ? "+" : "-") + "Create " : "",
                                        oAccess.Delete != nAceess.Delete ? (nAceess.Delete ? "+" : "-") + "Delete " : "",
                                        oAccess.Admin != nAceess.Admin ? (nAceess.Admin ? "+" : "-") + "Admin " : "",
                                        oAccess.Accept != nAceess.Accept ? (nAceess.Accept ? "+" : "-") + "Accept" : "");
                                }
                            }
                            foreach (var nAccess in newAcces.Items)
                            {
                                var access = oldAcces.Get(nAccess.Identity);
                                if (!access.Equals(AccessItem.Empty))
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
        }
    }
}
