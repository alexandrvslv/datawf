﻿using System;
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
        private ToolFieldEditor field;
        private ToolItem toolPrev;
        private ToolItem toolNext;

        public ListExplorer()
            : this(new ListEditor())
        { }

        public ListExplorer(ListEditor dataEditor)
        {
            field = new ToolFieldEditor();
            field.FillWidth = true;
            field.Field.BindData(Selection, nameof(ListExplorerCursor.Current), new CellEditorList());

            toolPrev = new ToolItem();
            toolPrev.Glyph = GlyphType.ArrowLeft;
            toolPrev.Click += ToolPrevClick;

            toolNext = new ToolItem();
            toolNext.Glyph = GlyphType.ArrowRight;
            toolNext.Click += ToolNextClick;

            Bar = new Toolsbar();
            Bar.Items.Add(toolPrev);
            Bar.Items.Add(toolNext);
            Bar.Items.Add(field);

            Editor = dataEditor;
            Editor.List.HideEmpty = false;
            Editor.List.EditMode = EditModes.ByClick;
            Editor.List.ReadOnly = false;
            Editor.Name = "fields";
            Editor.List.SelectionChanged += ListOnSelectionChanged;
            Editor.ItemSelect += OnEditorItemSelect;
            Editor.GetCellEditor += OnGetCellEditor;

            PackStart(Bar, false, false);
            PackStart(Editor, true, true);
            Name = "OptionEditor";

            Tree = new LayoutList();
            Tree = ((CellEditorList)field.Editor).DropDown.Target as LayoutList;
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
            get { return field.Visible; }
            set { field.Visible = value; }
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

        public Toolsbar Bar { get; protected set; }

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
                Text = Common.Locale.Get(DataSource.GetType().FullName, DataSource.GetType().Name),
                DataSource = DataSource
            };
            Tree.Nodes.Add(node);
            Tree.SelectedNode = node;
        }

        private Node InitNode(PropertyInfo info, object data)
        {
            return new ListExplorerNode()
            {
                Text = Common.Locale.Get(info.DeclaringType.FullName, info.Name),
                Name = info.Name,
                DataSource = data
            };
        }

        protected virtual void OnNodeSelect(ListExplorerNode node)
        {
            Selection.Current = node;
            field.Field.DropDown.Hide();
            field.DataValue = node;

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
            object obj = node.DataSource;

            foreach (var property in obj.GetType().GetProperties())
            {
                if (TypeHelper.GetBrowsable(property) && TypeHelper.IsList(property.PropertyType))
                {
                    Node propertyNode = InitNode(property, EmitInvoker.GetValue(property, obj));
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

                foreach (ListExplorerNode node in Tree.SelectedNode.Childs)
                {
                    if (node.Tag == e.Item)
                    {
                        Current = node;
                        return;
                    }
                }
                var newNode = new ListExplorerNode(e.Item.ToString())
                {
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
            toolPrev.Text = Common.Locale.Get("OptionEditor", "Prev");
            toolNext.Text = Common.Locale.Get("OptionEditor", "Next");
            Text = Common.Locale.Get("OptionEditor", "Option Editor");
            Editor.Localize();
        }
    }

}
