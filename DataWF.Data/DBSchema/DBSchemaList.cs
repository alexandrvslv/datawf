/*
 DBSchemaList.cs
 
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
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBSchemaList : DBSchemaItemList<DBSchema>
    {
        private DBSchema defaultSchema;

        public DBSchemaList() : base()
        { }

        public bool HandleChanges { get; set; } = true;

        [JsonIgnore, XmlIgnore]
        public DBSchema DefaultSchema
        {
            get
            {
                if (defaultSchema == null && Count > 0)
                    defaultSchema = this[0];
                return defaultSchema;
            }
            set
            {
                defaultSchema = value;
                if (defaultSchema != null && !Contains(defaultSchema))
                {
                    Add(defaultSchema);
                }
            }
        }

        public event EventHandler<EventArgs> ItemsListChanged;

        public override int AddInternal(DBSchema item)
        {
            int index = base.AddInternal(item);
            if (item.LogSchema != null && !Contains(item.LogSchema))
            {
                Add(item.LogSchema);
            }
            return index;
        }

        protected internal void OnItemsListChanged(object sender, EventArgs arg)
        {
            if (HandleChanges)
            {
                ItemsListChanged?.Invoke(sender, arg);
            }
        }

        public override void OnListChanged(NotifyCollectionChangedEventArgs args)
        {
            base.OnListChanged(args);
            OnItemsListChanged(this, args);
        }

        public IEnumerable<KeyValuePair<string, DBProcedure>> GetProcedures(string category = "General")
        {
            foreach (var schema in this)
            {
                foreach (var kvp in schema.Procedures.SelectByCategory(category))
                {
                    yield return kvp;
                }
            }
        }

        public DBProcedure ParseProcedure(string name, string category = "General")
        {
            var procedure = (DBProcedure)null;
            foreach (var schema in this)
            {
                procedure = schema.Procedures[name];
                if (procedure == null)
                    procedure = schema.Procedures.SelectByAttribute(name, category);
                if (procedure == null && category != "General")
                    procedure = schema.Procedures.SelectByAttribute(name);
                if (procedure != null)
                    break;
            }
            return procedure;
        }

        public void SaveCache()
        {
            foreach (DBSchema schema in this)
            {
                foreach (DBTable table in schema.Tables)
                {
                    if (table.Count > 0 && table.IsCaching && !(table is IDBVirtualTable))
                    {
                        table.SaveFile();
                    }
                }
            }
        }

        public void LoadCache()
        {
            foreach (DBSchema schema in this)
            {
                foreach (DBTable table in schema.Tables)
                {
                    if (table.IsCaching && !(table is IDBVirtualTable))
                    {
                        table.LoadFile();
                    }
                }
            }
            Helper.LogWorkingSet("Data Cache");
        }

        public DBColumn ParseColumn(string name, DBSchema schema = null)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            DBTable table = ParseTable(name, schema);

            int index = name.LastIndexOf('.');
            name = index < 0 ? name : name.Substring(index + 1);
            return table?.ParseColumn(name);
        }

        public DBTable ParseTableByTypeName(string code)
        {
            if (string.IsNullOrEmpty(code))
                return null;
            foreach (var schema in this)
            {
                var table = schema.Tables.FirstOrDefault(p => p.ItemType?.Type?.Name == code);
                if (table != null)
                    return table;
            }
            return null;
        }

        public DBTable ParseTable(string code, DBSchema s = null)
        {
            if (string.IsNullOrEmpty(code))
                return null;
            DBTable table = null;
            DBSchema schema = null;
            int index = code.IndexOf('.');
            if (index >= 0)
            {
                schema = this[code.Substring(0, index++)];
                int sindex = code.IndexOf('.', index);
                code = sindex < 0 ? code.Substring(index) : code.Substring(index, sindex - index);
            }
            if (schema == null)
                schema = s;
            if (schema != null)
            {
                table = schema.Tables[code];
            }
            else
            {
                foreach (var sch in this)
                {
                    table = sch.Tables[code];
                    if (table != null)
                        break;
                }
            }
            return table;
        }

        public DBTableGroup ParseTableGroup(string code, DBSchema s = null)
        {
            if (code == null)
                return null;
            DBSchema schema = null;
            int index = code.IndexOf('.');
            if (index < 0)
                schema = s;
            else
            {
                schema = this[code.Substring(0, index++)];
                int sindex = code.IndexOf('.', index);
                code = sindex < 0 ?
                    code.Substring(index) :
                    code.Substring(index, sindex - index);
            }
            return schema.TableGroups[code];
        }

        public void Deserialize(string file, DBSchemaItem selectedItem)
        {
            var item = Serialization.Deserialize(file);
            if (item is DBTable table)
            {
                DBSchema schema = selectedItem.Schema;

                if (schema.Tables.Contains(table.Name))
                    schema.Tables.Remove(table.Name);
                schema.Tables.Add(table);
            }
            else if (item is DBSchema schema)
            {
                if (Contains(schema.Name))
                    schema.Name = schema.Name + "1";
                Add((DBSchema)item);
            }
            else if (item is DBColumn column)
            {
                if (selectedItem is DBTable sTable)
                    sTable.Columns.Add((DBColumn)item);
            }
            else if (item is SelectableList<DBSchemaItem> list)
            {
                foreach (var element in list)
                {
                    if (element is DBColumn && selectedItem is DBTable)
                        ((DBTable)selectedItem).Columns.Add((DBColumn)element);
                    else if (element is DBTable && selectedItem is DBSchema)
                        ((DBSchema)selectedItem).Tables.Add((DBTable)element);
                }

            }
        }

        [Invoker(typeof(DBSchemaList), nameof(DBSchemaList.HandleChanges))]
        public class HandleChangesInvoker<T> : Invoker<T, bool> where T : DBSchemaList
        {
            public static readonly HandleChangesInvoker<T> Instance = new HandleChangesInvoker<T>();
            public override string Name => nameof(DBSchemaList.HandleChanges);

            public override bool CanWrite => true;

            public override bool GetValue(T target) => target.HandleChanges;

            public override void SetValue(T target, bool value) => target.HandleChanges = value;
        }
    }
}
