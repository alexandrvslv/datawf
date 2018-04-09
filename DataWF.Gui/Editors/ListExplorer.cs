using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{

    public class ListExplorer : VPanel, IDockContent
    {
        private object dataSource;
        private ToolFieldEditor toolTree;
        private ToolItem toolPrev;
        private ToolItem toolNext;

        public ListExplorer()
            : this(new ListEditor())
        { }

        public ListExplorer(ListEditor dataEditor)
        {
            Editor = dataEditor;
            Editor.List.HideEmpty = false;
            Editor.List.EditMode = EditModes.ByClick;
            Editor.List.ReadOnly = false;
            Editor.Name = "fields";
            Editor.List.SelectionChanged += ListOnSelectionChanged;
            Editor.ItemSelect += OnEditorItemSelect;
            Editor.GetCellEditor += OnGetCellEditor;

            toolTree = new ToolFieldEditor() { FillWidth = true };
            toolTree.Field.BindData(Selection, nameof(ListExplorerCursor.Current), new CellEditorList());
            toolPrev = new ToolItem(ToolPrevClick) { Glyph = GlyphType.ArrowLeft };
            toolNext = new ToolItem(ToolNextClick) { Glyph = GlyphType.ArrowRight };

            var group = new ToolLayoutMap();
            group.Add(toolPrev);
            group.Add(toolNext);
            group.Add(toolTree);

            Bar.Items.Insert(group, true);

            PackStart(Editor, true, true);
            Name = "OptionEditor";

            Tree = ((CellEditorList)toolTree.Editor).DropDown.Target as LayoutList;
            Tree.Mode = LayoutListMode.Tree;
            Tree.SelectionChanged += OnTreeAfterSelect;
        }

        public bool IsModule
        {
            get { return false; }
        }

        public bool HideOnClose
        {
            get { return false; }
        }

        public DockType DockType
        {
            get { return DockType.Content; }
        }

        public ListExplorerNode Current
        {
            get { return Tree.SelectedNode as ListExplorerNode; }
            set
            {
                if (value.Container == null)
                    Tree.Nodes.Add(value);
                Tree.SelectedNode = value;
            }
        }

        public object Picture
        {
            get { return Locale.GetImage("OptionEditor", "Option Editor"); }
        }

        public bool ReadOnly
        {
            get { return Editor.ReadOnly; }
            set { Editor.ReadOnly = value; }
        }

        public bool ViewTree
        {
            get { return toolTree.Visible; }
            set { toolTree.Visible = value; }
        }

        public object Value
        {
            get { return DataSource; }
            set
            {
                if (DataSource == value)
                    return;
                DataSource = value;
                InitializeTree();
            }
        }

        public ListExplorerCursor Selection { get; protected set; } = new ListExplorerCursor();

        public LayoutList Tree { get; protected set; }

        public ListEditor Editor { get; protected set; }

        public Toolsbar Bar { get { return Editor.Bar; } }

        public object DataSource
        {
            get { return dataSource; }
            set
            {
                if (dataSource == value)
                    return;
                dataSource = value;
                InitializeTree();
            }
        }

        public event PListGetEditorHandler GetCellEditor;

        protected ILayoutCellEditor OnGetCellEditor(object listItem, object value, ILayoutCell cell)
        {
            return GetCellEditor?.Invoke(listItem, value, cell);
        }

        private void ToolNextClick(object sender, EventArgs e)
        {
            Current = Selection.Next();
        }

        private void ToolPrevClick(object sender, EventArgs e)
        {
            Current = Selection.Prev();
        }

        private void InitializeTree()
        {
            Tree.Nodes.Clear();
            Tree.Tag = DataSource;
            if (DataSource == null)
            {
                Editor.DataSource = null;
                return;
            }

            var node = new ListExplorerNode()
            {
                Text = Locale.Get(DataSource.GetType().FullName, DataSource.GetType().Name),
                DataSource = DataSource
            };
            Tree.Nodes.Add(node);
            Tree.SelectedNode = node;
        }

        private Node InitNode(PropertyInfo info, object data)
        {
            return new ListExplorerNode()
            {
                Text = Locale.Get(info.DeclaringType.FullName, info.Name),
                Name = info.Name,
                DataSource = data
            };
        }

        protected virtual void OnNodeSelect(ListExplorerNode node)
        {
            Text = node.ToString();
            Selection.Current = node;
            toolTree.Field.DropDown.Hide();
            toolTree.DataValue = node;

            node.Apply(Editor);

            if (!node.Check)
            {
                OnNodeCheck(node);
            }
        }

        protected virtual void OnNodeCheck(ListExplorerNode node)
        {
            if (node.DataSource == null)
                return;

            foreach (var property in node.DataSource.GetType().GetProperties())
            {
                if (TypeHelper.GetBrowsable(property) && TypeHelper.IsList(property.PropertyType) && !TypeHelper.IsIndex(property))
                {
                    Node propertyNode = InitNode(property, EmitInvoker.GetValue(property, node.DataSource));
                    if (propertyNode != null)
                    {
                        propertyNode.Group = node;
                    }
                    Tree.Nodes.Add(propertyNode);
                }
            }
            node.Check = true;
        }

        protected virtual void OnTreeAfterSelect(object sender, EventArgs e)
        {
            if (Current == null)
                return;
            OnNodeSelect(Current);
        }

        protected virtual void OnEditorItemSelect(object sender, ListEditorEventArgs e)
        {
            if (Current != null)
            {
                e.Cancel = true;

                foreach (ListExplorerNode node in Tree.SelectedNode.Nodes)
                {
                    if (node.DataSource == e.Item)
                    {
                        Current = node;
                        return;
                    }
                }
                var newNode = new ListExplorerNode(e.Item.ToString())
                {
                    Text = e.Item.ToString(),
                    DataSource = e.Item,
                    Group = Current
                };
                Current = newNode;
            }
        }

        private void ListOnSelectionChanged(object sender, EventArgs e)
        {
        }

        public virtual void Localize()
        {
            toolPrev.Text = Locale.Get("OptionEditor", "Prev");
            toolNext.Text = Locale.Get("OptionEditor", "Next");
            Editor.Localize();
        }
    }

}
