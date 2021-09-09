//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public partial class DBSchemaList : DBSchemaItemList<DBSchema>
    {
        private DBSchema defaultSchema;

        public DBSchemaList() : base()
        { }

        [JsonIgnore, XmlIgnore]
        public DBProvider Provider
        {
            get;
            set;
        }

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
                Add((DBSchema)item.LogSchema);
            }
            return index;
        }

        protected internal void OnItemsListChanged(object sender, EventArgs arg)
        {
            ItemsListChanged?.Invoke(sender, arg);
        }

        public override NotifyCollectionChangedEventArgs OnCollectionChanged(NotifyCollectionChangedAction type, object item = null, int index = -1, int oldIndex = -1, object oldItem = null)
        {
            var args = base.OnCollectionChanged(type, item, index, oldIndex, oldItem);
            OnItemsListChanged(this, args ?? (args = ListHelper.GenerateArgs(type, item, index, oldIndex, oldItem)));
            return args;
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
                    if (table.Count > 0 && table.IsCaching && !table.IsVirtual)
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
                    if (table.IsCaching && !table.IsVirtual)
                    {
                        table.LoadFile();
                    }
                }
            }
            Helper.LogWorkingSet("Data Cache");
        }

        public DBColumn ParseColumn(string name, IDBSchema schema = null)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            DBTable table = ParseTable(name, schema);

            int index = name.LastIndexOf('.');
            name = index < 0 ? name : name.Substring(index + 1);
            return table?.GetColumn(name);
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

        public DBTable ParseTable(string code, IDBSchema s = null)
        {
            if (string.IsNullOrEmpty(code))
                return null;
            DBTable table = null;
            IDBSchema schema = null;
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

        public void Deserialize(string file, IDBSchemaItem selectedItem)
        {
            var item = Serialization.Deserialize(file);
            if (item is DBTable table)
            {
                var schema = selectedItem.Schema;

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
    }
}
