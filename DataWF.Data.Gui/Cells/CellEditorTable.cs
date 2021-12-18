using DataWF.Common;
using DataWF.Gui;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Xwt;

namespace DataWF.Data.Gui
{
    public class CellEditorTable : CellEditorList
    {
        protected IDBTable table;
        protected DBColumn column;
        protected string viewFilter = string.Empty;

        public CellEditorTable()
            : base()
        {
            DropDownExVisible = true;
            dropDownAutoHide = true;
            Filtering = true;
        }

        public string ViewFilter
        {
            get { return viewFilter; }
            set { viewFilter = value; }
        }

        public DBColumn Column
        {
            get { return column; }
            set { column = value; }
        }

        public TableEditor TableEditor
        {
            get { return DropDown?.Target as TableEditor; }
        }

        public override LayoutList List
        {
            get { return TableEditor?.List; }
        }

        public IDBTable Table
        {
            get { return table; }
            set
            {
                table = value;
                if (TableEditor != null && listSource is IDBTableView && ((IDBTableView)listSource).Table != table)
                {
                    View.Dispose();
                    listSource = null;
                    //View = table.CreateView(viewFilter, DBViewInitMode.None, DBStatus.Current);
                    TableEditor.Initialize(View, GetItem(Value), column, TableEditorMode.Reference, false);
                }
            }
        }

        public IDBTableView View
        {
            get
            {
                if ((listSource == null || ((IDBTableView)listSource).Disposed) && table != null)
                    listSource = table.CreateView(viewFilter, DBViewKeys.None, DBStatus.Actual | DBStatus.New | DBStatus.Edit);
                return listSource as IDBTableView;
            }
            set
            {
                listSource = value;
                if (listSource is IDBTableView && ((IDBTableView)listSource).Table != table)
                    table = View.Table;
            }
        }

        public DBItem GetItem(object obj)
        {
            return GetItem(obj, EditItem);
        }

        public DBItem GetItem(object obj, object source)
        {
            if (!(obj is DBItem))
                if (source is DBItem && column != null)
                {
                    var row = (DBItem)source;
                    if (row[column]?.Equals(obj) ?? false)
                    {
                        obj = row.GetReference(column, DBLoadParam.None);
                        if (obj == null)
                        {
                            getReferenceStack.Push(new PDBTableParam() { Row = row, Column = column });
                            if (getReferenceStack.Count == 1)
                            {
                                ThreadPool.QueueUserWorkItem(p => LoadReference());
                            }
                            obj = DBItem.EmptyItem;
                        }
                    }
                    else
                        obj = column.ReferenceTable.LoadById<DBItem>(obj);
                }
                else if (table != null)
                    obj = table.LoadById<DBItem>(obj);
            return obj as DBItem;
        }

        public struct PDBTableParam
        {
            public DBItem Row;
            public DBColumn Column;
        }

        private ConcurrentStack<PDBTableParam> getReferenceStack = new ConcurrentStack<PDBTableParam>();

        private void LoadReference()
        {
            using (var transaction = new DBTransaction(Table, GuiEnvironment.User))
            {
                Debug.WriteLine("Get References {0}", getReferenceStack.Count);
                while (getReferenceStack.TryPop(out PDBTableParam item))
                {
                    item.Row.GetReference(item.Column, DBLoadParam.Load);
                    //item.Row.OnPropertyChanged(item.Column.Name, item.Column);
                }
            }
        }

        public override object FormatValue(object value, object dataSource, Type valueType)
        {
            if (value == null || value == DBNull.Value)
                return null;
            value = GetItem(value, dataSource);

            return base.FormatValue(value, dataSource, valueType);
        }

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            if (value != null && (value.GetType() == valueType))
                return value;
            if (value is string)
            {
                value = table.LoadById<DBItem>(value, DBLoadParam.None);
            }
            if (value is DBItem)
            {
                if (TypeHelper.IsBaseType(value.GetType(), valueType))
                    return value;
                return ((DBItem)value).PrimaryId;
            }
            return base.ParseValue(value, dataSource, valueType);
        }

        public override Widget InitDropDownContent()
        {
            var tableEditor = Editor.GetCached<TableEditor>();

            tableEditor.ReadOnly = ReadOnly;
            tableEditor.KeyPressed -= OnTextKeyPressed;

            if (table != null)
            {
                Editor.DropDownExClick += OnDropDownExClick;
                tableEditor.Initialize(View, GetItem(Value), column, TableEditorMode.Reference, false);
                if (!ReadOnly)
                {
                    tableEditor.ItemSelect += OnTableControlRowSelect;
                }
                if (viewFilter != null && viewFilter.Length > 0)
                {
                    View.DefaultParam = new QParam((DBTable)table, viewFilter);
                }
            }
            return tableEditor;
        }

        private void OnDropDownExClick(object sender, EventArgs e)
        {
            var row = GetItem(Editor.Value, EditItem);
            if (Editor != null && row != null)
            {
                using (var te = new TableExplorer())
                {
                    te.Initialize(row, TableEditorMode.Item, false);
                    te.ShowDialog(Editor);
                }
            }
        }

        protected override void ListReset()
        {
            View.ResetFilter();
        }

        protected override IEnumerable ListFind(string filter)
        {
            IEnumerable list = null;

            if (Table.CodeKey != null)
            {
                DBItem item = Table.LoadByCode<DBItem>(filter.Trim(), Table.CodeKey, Table.IsSynchronized ? DBLoadParam.None : DBLoadParam.Load);
                if (item != null)
                    list = new object[] { item };
            }
            if (list == null)
            {
                var query = Table.QQuery("");
                query.WhereViewColumns(EntryText);
                TableEditor.Loader.LoadAsync(query);
                list = query.Select<DBItem>();
            }
            return list;
        }

        protected override void ListSelect(IEnumerable flist)
        {
            if (flist != null)
            {
                //((TableEditor)tool.Target).List.SelectedValues._Clear();
                //((TableEditor)tool.Target).List.SelectedValues.AddRange(flist);
                //((TableEditor)tool.Target).List.VScrollToItem(flist[0]);
                foreach (var item in flist)
                {
                    TableEditor.List.SelectedItem = item;
                    break;
                }
            }
            else
            {
                //((TableEditor)tool.Target).List.SelectedValues.Clear();
            }
        }

        private void OnTableControlRowSelect(object sender, ListEditorEventArgs e)
        {
            if (TableEditor != null)
            {
                var item = (DBItem)e.Item;
                Value = ParseValue(item);
                ((TextEntry)Editor.Widget).Changed -= OnTextChanged;
                ((TextEntry)Editor.Widget).Text = item.ToString();
                ((TextEntry)Editor.Widget).Changed += OnTextChanged;
                DropDown.Hide();
            }
        }

        public override void FreeEditor()
        {
            if (Editor != null)
            {
                Editor.DropDownExClick -= OnDropDownExClick;
            }
            if (TableEditor != null)
            {
                TableEditor.ItemSelect -= OnTableControlRowSelect;
            }
            base.FreeEditor();
        }

    }
}

