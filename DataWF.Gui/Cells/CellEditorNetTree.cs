using DataWF;
using System;
using System.Net;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace DataWF.Gui
{
    public class CellEditorNetTree : CellEditorText
    {
        public CellEditorNetTree()
            : base()
        {
            //handleText = false;
            dropDownAutoHide = true;
            Filtering = true;
        }

        public NetTree Tree
        {
            get { return DropDown?.Target as NetTree; }
        }

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            if (value is string text && text.Length > 12)
            {
                if (IPAddress.TryParse((string)value, out IPAddress temp))
                {
                    return temp;
                }
            }
            return base.ParseValue(value, dataSource, valueType);
        }

        protected override void SetFilter(string filter)
        {
            //base.SetFilter(filter);
            base.filter = filter;
            var list = Tree.ListSource as SelectableListView<Node>;

            list.FilterQuery.Parameters.Clear();
            if (filter.Length > 0)
            {
                list.FilterQuery.Parameters.Add(CompareOperator.And, LayoutNodeList<Node>.FullPathInvoker, CompareOperator.Like, filter);
            }
            else
            {
                list.FilterQuery.Parameters.Add(CompareOperator.Undefined, TreeInvoker<Node>.Instance, CompareOperator.Equal, true);
            }
            list.UpdateFilter();
            if (list.Count == 1 && list[0].Tag.GetType() == typeof(IPAddress))
            {

                string value = FormatValue(list[0].Tag, EditItem, DataType) as string;
                int index = value.IndexOf(filter, StringComparison.OrdinalIgnoreCase);
                EntryControl.Text = value;
                EntryControl.SelectionStart = index + filter.Length;
                EntryControl.SelectionEnd = value.Length;

                Value = list[0].Tag;
            }
            else if (filter.Length > 0)
            {
                Editor.ShowDropDown(ToolShowMode.AutoHide);
            }
            HandleText = true;
        }

        public override IControl InitDropDownContent()
        {
            var tree = Editor.GetCached<NetTree>();

            tree.Localize();
            foreach (Node n in tree.Nodes.GetTopLevel())
                n.Expand = true;

            if (Value is IPAddress)
            {
                tree.SelectedNode = tree.Nodes.Find(Value.GetHashCode().ToString());
            }
            if (!ReadOnly)
            {
                tree.CellDoubleClick += HandleAfterSelect;
                tree.KeyDown += TreeCellKeyDown;
            }
            return tree;
        }

        protected void TreeCellKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                var node = ((LayoutList)sender).SelectedNode;
                Value = node.Tag;
                DropDown.Hide();
            }
        }

        protected virtual void HandleAfterSelect(object sender, EventArgs e)
        {
            if (Editor != null && sender is NetTree netTree &&
                (netTree.SelectedNode?.Tag?.GetType() == typeof(IPAddress)))
            {
                Value = netTree.SelectedNode.Tag;
            }
        }

        public override void FreeEditor()
        {
            if (Tree != null)
            {
                Tree.CellDoubleClick -= HandleAfterSelect;
                Tree.KeyDown -= TreeCellKeyDown;
            }
            base.FreeEditor();
        }
    }
}
