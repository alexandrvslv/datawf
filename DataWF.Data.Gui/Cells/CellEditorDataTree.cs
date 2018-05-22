using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using System;
using Xwt;

namespace DataWF.Data.Gui
{
    public class CellEditorDataTree : CellEditorText
    {
        protected DataTreeKeys key = DataTreeKeys.None;

        public CellEditorDataTree()
            : base()
        {
            //handleText = false;
            dropDownAutoHide = true;
            //filtering = true;
        }

        public DataTree DataTree
        {
            get { return DropDown?.Target as DataTree; }
        }

        public DBSchemaItem DataFilter { get; set; }

        public DataTreeKeys DataKeys
        {
            get { return key; }
            set { key = value; }
        }

        public override Type DataType
        {
            get { return base.DataType; }
            set
            {
                if (DataType == value)
                    return;
                base.DataType = value;
                if (value?.IsGenericType ?? false)
                {
                    base.DataType = value.BaseType;
                }
                if (value == typeof(DBSchema))
                    key = DataTreeKeys.Schema;
                else if (value == typeof(DBProcedure))
                    key = DataTreeKeys.Schema | DataTreeKeys.Procedure;
                else if (value == typeof(DBSequence))
                    key = DataTreeKeys.Schema | DataTreeKeys.Sequence;
                else if (value == typeof(DBTableGroup))
                    key = DataTreeKeys.Schema | DataTreeKeys.TableGroup;
                else if (value == typeof(DBTable))
                    key = DataTreeKeys.Schema | DataTreeKeys.TableGroup | DataTreeKeys.Table;
                else if (value == typeof(DBColumnGroup))
                    key = DataTreeKeys.Schema | DataTreeKeys.TableGroup | DataTreeKeys.Table | DataTreeKeys.ColumnGroup;
                else if (value == typeof(DBColumn))
                    key = DataTreeKeys.Schema | DataTreeKeys.TableGroup | DataTreeKeys.Table | DataTreeKeys.ColumnGroup | DataTreeKeys.Column;
            }
        }

        protected override void SetFilter(string filter)
        {
            //base.SetFilter(filter);
            base.filter = filter;
            var list = DataTree.ListSource as SelectableListView<Node>;

            list.FilterQuery.Parameters.Clear();
            if (filter.Length > 0)
                list.FilterQuery.Parameters.Add(typeof(Node), LogicType.And, nameof(Node.FullPath), CompareType.Like, filter);
            else
                list.FilterQuery.Parameters.Add(typeof(Node), LogicType.Undefined, nameof(Node.IsExpanded), CompareType.Equal, true);
            list.UpdateFilter();
            if (list.Count == 1 && list[0].Tag.GetType() == DataType)
            {
                string value = FormatValue(list[0].Tag, EditItem, DataType) as string;
                int index = value.IndexOf(filter, StringComparison.OrdinalIgnoreCase);
                TextWidget.Text = value;
                TextWidget.SelectionStart = index + filter.Length;
                TextWidget.SelectionLength = value.Length - TextWidget.SelectionStart;
                editor.Value = list[0].Tag;
            }
            else if (filter.Length > 0)
            {
                editor.ShowDropDown(ToolShowMode.AutoHide);
            }
            HandleText = true;
        }

        public virtual DataTree GetToolTarget()
        {
            return editor.GetCached<DataTree>();
        }

        public override Widget InitDropDownContent()
        {
            if (EditItem is DBTable && DataType == typeof(DBTableGroup))
            {
                DataFilter = ((DBTable)EditItem).Schema;
            }
            else if (EditItem is DBTable && DataType == typeof(DBTable))
            {
                DataFilter = ((DBTable)EditItem).Schema;
            }
            else if (EditItem is DBTableGroup && DataType == typeof(DBTableGroup))
            {
                DataFilter = ((DBTableGroup)EditItem).Schema;
            }
            else if (EditItem is DBColumn && DataType == typeof(DBTable))
            {
                //datafilter = ((DBColumn)dataSource).Schema;
            }
            else if (EditItem is DBColumn && DataType == typeof(DBColumnGroup))
            {
                DataFilter = ((DBColumn)EditItem).Table;
            }
            else if (EditItem is DBTable && DataType == typeof(DBColumn))
            {
                DataFilter = (DBTable)EditItem;
            }
            else if (EditItem is DBVirtualColumn)
            {
                DataFilter = ((DBVirtualColumn)EditItem).VirtualTable.BaseTable;
            }

            var tree = GetToolTarget();
            tree.DataKeys = key;
            tree.DataFilter = DataFilter;
            tree.Localize();
            foreach (Node n in tree.Nodes.GetTopLevel())
                n.Expand = true;

            if (Value is DBSchemaItem)
            {
                tree.SelectedNode = tree.Nodes.Find(DataTree.GetName(Value));
            }
            if (!ReadOnly)
            {
                tree.CellDoubleClick += HandleAfterSelect;
                tree.KeyPressed += TreeCellKeyDown;
            }
            return tree;
        }

        protected void TreeCellKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.NumPadEnter || e.Key == Key.Return)
            {
                Value = DataTree.SelectedDBItem;
                DropDown.Hide();
            }
        }

        protected virtual void HandleAfterSelect(object sender, EventArgs e)
        {
            if (editor != null
                && DataTree.SelectedDBItem != null
                && (TypeHelper.IsBaseType(DataTree.SelectedDBItem.GetType(), DataType) || DataType == null))
            {
                Value = DataTree.SelectedDBItem; 
            }
        }

        protected override object GetDropDownValue()
        {
            return DataTree.SelectedDBItem;
        }

        public override void FreeEditor()
        {
            if (DataTree is DataTree)
            {
                DataTree.CellDoubleClick -= HandleAfterSelect;
                DataTree.KeyPressed -= TreeCellKeyDown;
            }
            base.FreeEditor();
        }
    }
}


