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
using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;

namespace DataWF.Module.Common
{
    public class UserLogItem
    {
        private DBLogItem cacheLogItem;
        private DBTable cacheTargetTable;
        private DBItem cacheTarget;

        [Browsable(false)]
        public string TableName { get; set; }

        [XmlIgnore]
        public DBTable Table
        {
            get { return cacheTargetTable ?? (cacheTargetTable = DBService.ParseTable(TableName)); }
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
            get { return cacheTarget ?? (cacheTarget = (Table?.LoadItemById(ItemId) ?? DBItem.EmptyItem)); }
            set
            {
                cacheTarget = value;
                ItemId = value?.PrimaryId.ToString();
                Table = value?.Table;
            }
        }

        public DBLogTable LogTable
        {
            get { return Table?.LogTable; }
        }

        [Browsable(false)]
        public int? LogId { get; set; }

        [XmlIgnore]
        public DBLogItem LogItem
        {
            get { return cacheLogItem ?? (cacheLogItem = LogTable?.LoadById(LogId)); }
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

        public void RefereshText()
        {
            string _textCache = "";
            if (_textCache?.Length == 0 && LogItem != null)
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
        }
    }
}
