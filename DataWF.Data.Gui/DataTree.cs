using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;

namespace DataWF.Data.Gui
{
    [Flags]
    public enum DataTreeKeys
    {
        None = 0,
        Schema = 1,
        TableGroup = 2,
        Table = 4,
        ColumnGroup = 8,
        Column = 16,
        Index = 32,
        Constraint = 64,
        Foreign = 128,
        Procedure = 256,
        ProcedureParam = 512,
        CheckTableView = 1024,
        CheckTableEdit = 2048
    }

    public class DataTree : LayoutList
    {
        private ListChangedEventHandler schemaChanged;
        private object datafilter = null;
        DataTreeKeys dataKeys = DataTreeKeys.None;
        //TODO
        //private bool checkDelete = false;

        public DataTree()
            : base()
        {
            schemaChanged = new ListChangedEventHandler(OnItemsListChanged);
            base.ListSensetive = true;
            Mode = LayoutListMode.Tree;
        }

        public DataTreeKeys DataKeys
        {
            get { return dataKeys; }
            set
            {
                if (dataKeys != value)
                {
                    dataKeys = value;
                    RefreshData();
                }
            }
        }

        public override void Localize()
        {
            base.Localize();
            LocalizeNodes();
        }

        public virtual void LocalizeNodes()
        {
            if (Nodes != null)
                foreach (var node in Nodes)
                {
                    if (node.Tag is DBSchemaItem)
                        node.Text = node.Tag.ToString();
                }
        }

        protected virtual void RefreshData()
        {
            if (datafilter != null)
            {
                Nodes.Add(InitObject(datafilter));
            }
            else
            {
                InitSchems();
            }
        }

        public object DataFilter
        {
            get { return datafilter; }
            set
            {
                if (datafilter != value)
                {
                    datafilter = value;
                    Nodes.Clear();
                    RefreshData();
                }
            }
        }

        private void InitSchems()
        {
            DBService.Schems.ListChanged -= schemaChanged;
            if (ShowSchema)
                DBService.Schems.ListChanged += schemaChanged;

            Nodes.Sense = false;
            foreach (DBSchema schema in DBService.Schems)
            {
                Node node = Find(schema);
                if (ShowSchema)
                {
                    node = InitSchema(schema);
                    Nodes.Add(node);
                }
                else if (node != null)
                    node.Hide();
            }
            Nodes.Sense = true;
        }

        [DefaultValue(false)]
        public bool ShowSchema
        {
            get { return (dataKeys & DataTreeKeys.Schema) == DataTreeKeys.Schema; }
        }

        [DefaultValue(false)]
        public bool ShowTableGroup
        {
            get { return (dataKeys & DataTreeKeys.TableGroup) == DataTreeKeys.TableGroup; }
        }

        [DefaultValue(true)]
        public bool CheckTableView
        {
            get { return (dataKeys & DataTreeKeys.CheckTableView) == DataTreeKeys.CheckTableView; }
        }

        [DefaultValue(false)]
        public bool CheckTableEdit
        {
            get { return (dataKeys & DataTreeKeys.CheckTableEdit) == DataTreeKeys.CheckTableEdit; }
        }

        [DefaultValue(false)]
        public bool ShowTable
        {
            get { return (dataKeys & DataTreeKeys.Table) == DataTreeKeys.Table; }
        }

        [DefaultValue(false)]
        public bool ShowColumnGroup
        {
            get { return (dataKeys & DataTreeKeys.ColumnGroup) == DataTreeKeys.ColumnGroup; }
        }

        [DefaultValue(false)]
        public bool ShowColumn
        {
            get { return (dataKeys & DataTreeKeys.Column) == DataTreeKeys.Column; }
        }

        [DefaultValue(false)]
        public bool ShowIndex
        {
            get { return (dataKeys & DataTreeKeys.Index) == DataTreeKeys.Index; }
        }

        [DefaultValue(false)]
        public bool ShowConstraint
        {
            get { return (dataKeys & DataTreeKeys.Constraint) == DataTreeKeys.Constraint; }
        }

        [DefaultValue(false)]
        public bool ShowForeign
        {
            get { return (dataKeys & DataTreeKeys.Foreign) == DataTreeKeys.Foreign; }
        }

        [DefaultValue(true)]
        public bool ShowProcedures
        {
            get { return (dataKeys & DataTreeKeys.Procedure) == DataTreeKeys.Procedure; }
        }

        [DefaultValue(false)]
        public bool ShowProcedureParam
        {
            get { return (dataKeys & DataTreeKeys.ProcedureParam) == DataTreeKeys.ProcedureParam; }
        }


        public void OnItemsListChanged(object sender, ListChangedEventArgs arg)
        {
            if (arg.ListChangedType == ListChangedType.Reset)
            {
                RefreshData();
                return;
            }

            if (arg.ListChangedType == ListChangedType.ItemAdded ||
                arg.ListChangedType == ListChangedType.ItemChanged ||
                (arg.ListChangedType == ListChangedType.ItemDeleted && arg.NewIndex >= 0))
            {
                var item = ((IList)sender)[arg.NewIndex] as DBSchemaItem;
                var node = InitObject(item);
                if (arg.ListChangedType != ListChangedType.ItemDeleted)
                {
                    Node onode = item is IDBTableContent ? Find(((IDBTableContent)item).Table) : Find(item.Schema);
                    if (item is DBTableGroup && ((DBTableGroup)item).Group != null && ShowTableGroup)
                        onode = Find(((DBTableGroup)item).Group);
                    else if (item is DBTable && ((DBTable)item).Group != null && ShowTableGroup)
                        onode = Find(((DBTable)item).Group);
                    else if (item is DBColumn && ((DBColumn)item).Group != null && ShowColumnGroup)
                        onode = Find(((DBColumn)item).Group);
                    if (onode != null)
                        node.Group = onode;
                    Nodes.Add(node);
                }
                else
                    Nodes.Remove(node);
            }
        }

        public Node Find(object obj)
        {
            if (obj == null)
                return null;
            return Find(GetName(obj));
        }

        public Node Find(string name)
        {
            return Nodes.SelectOne("Name", CompareType.Equal, name);
        }

        public virtual Node InitObject(object obj)
        {
            Node node = null;
            if (obj is DBSchema)
                node = InitSchema((DBSchema)obj);
            else if (obj is DBTableGroup)
                node = InitTableGroup((DBTableGroup)obj);
            else if (obj is DBTable)
                node = InitTable((DBTable)obj);
            else if (obj is DBColumnGroup)
                node = InitColumnGroup((DBColumnGroup)obj);
            else if (obj is DBColumn)
                node = InitColumn((DBColumn)obj);
            else
                node = InitItem(obj);

            return node;
        }

        public object SelectedObject
        {
            get { return SelectedNode?.Tag; }
        }

        public DBSchema CurrentSchema
        {
            get
            {
                return SelectedObject is DBSchema
                ? (DBSchema)SelectedObject
                : SelectedObject is DBSchemaItem
                    ? ((DBSchemaItem)SelectedObject).Schema
                    : null;
            }
        }

        public virtual string GetName(object obj)
        {
            string str = "";
            if (obj != null)
                str = obj.GetType().FullName + obj.GetHashCode();

            return str;
        }


        public Node InitSchema(DBSchema schema)
        {
            Node node = InitItem(schema);

            schema.TableGroups.ListChanged -= schemaChanged;
            schema.Tables.ListChanged -= schemaChanged;
            schema.Procedures.ListChanged -= schemaChanged;

            if (ShowTableGroup)
                schema.TableGroups.ListChanged += schemaChanged;
            if (ShowTable)
                schema.Tables.ListChanged += schemaChanged;
            if (ShowProcedures)
                schema.Procedures.ListChanged += schemaChanged;

            foreach (var tgoup in schema.TableGroups.GetTopParents())
            {
                Node gnode = Find(tgoup);
                if (ShowTableGroup)
                {
                    gnode = InitTableGroup(tgoup);
                    gnode.Group = node;
                }
                else if (gnode != null)
                    gnode.Hide();
            }

            foreach (var table in schema.Tables)
            {
                if (table.Group != null && ShowTableGroup)
                    continue;
                var tnode = Find(table);
                if (ShowTable &&
                    (!CheckTableView || (CheckTableView && table.Access.View)) &&
                    (!CheckTableEdit || (CheckTableEdit && table.Access.Admin)))
                {
                    tnode = InitTable(table);
                    tnode.Group = node;
                }
                else if (tnode != null)
                    tnode.Hide();
            }

            foreach (var procedure in schema.Procedures.SelectByParent(null))
            {
                Node gnode = Find(procedure);
                if (ShowProcedures)
                {
                    gnode = InitProcedure(procedure);
                    gnode.Group = node;
                }
                else if (gnode != null)
                    gnode.Hide();
            }
            return node;
        }

        public void InitList<T>(IEnumerable<T> list, Node parentNode, bool show)
        {
            foreach (var item in list)
            {
                if (show)
                {
                    var itemNode = InitItem(item);
                    itemNode.Group = parentNode;
                }
                else
                {
                    var itemNode = Find(item);
                    if (itemNode != null)
                        itemNode.Hide();
                }
            }
        }

        public Node InitProcedure(DBProcedure procedure)
        {
            Node node = InitItem(procedure);
            var list = procedure.Parameters;
            InitList<DBProcParameter>(list, node, ShowProcedureParam);
            foreach (var sprocedure in procedure.Childs)
            {
                InitProcedure(sprocedure).Group = node;
            }
            return node;
        }

        public Node InitTableGroup(DBTableGroup tgroup)
        {
            var node = InitItem(tgroup);

            foreach (var item in tgroup.Childs)
            {
                var gnode = Find(item);
                if (ShowTableGroup)
                {
                    gnode = InitTableGroup(item);
                    gnode.Group = node;
                }
                else if (gnode != null)
                    gnode.Hide();
            }

            var tables = tgroup.GetTables();

            foreach (var table in tables)
            {
                var tnode = Find(table);
                if (ShowTable &&
                    (!CheckTableView || (CheckTableView && table.Access.View)) &&
                    (!CheckTableEdit || (CheckTableEdit && table.Access.Admin)))
                {
                    tnode = InitTable(table);
                    tnode.Group = node;
                }
                else if (tnode != null)
                    tnode.Hide();
            }
            return node;
        }


        public Node InitTable(DBTable table)
        {
            Node node = InitItem(table);
            InitColumnGroups(table, node);
            InitColumns(table, node);
            InitIndexes(table, node);
            InitConstraints(table, node);
            InitForeigns(table, node);
            return node;
        }

        public void InitColumns(DBTable table, Node node)
        {
            table.Columns.ListChanged -= schemaChanged;
            if (ShowColumn)
                table.Columns.ListChanged += schemaChanged;

            foreach (DBColumn column in table.Columns)
            {
                if (column.Group != null && ShowColumnGroup)
                    continue;
                var cnode = Find(column);
                if (ShowColumn && column.Access.View)
                {
                    if (cnode == null)
                        cnode = InitColumn(column);
                    else
                        cnode.Visible = true;
                    cnode.Group = node;
                }
                else if (cnode != null)
                    cnode.Hide();
            }
        }

        public void InitIndexes(DBTable table, Node node)
        {
            table.Indexes.ListChanged -= schemaChanged;
            if (ShowIndex)
                table.Indexes.ListChanged += schemaChanged;

            foreach (var index in table.Indexes)
            {
                var inode = Find(index);
                if (ShowIndex)
                {
                    if (inode == null)
                        inode = InitItem(index);
                    else
                        inode.Visible = true;
                    inode.Group = node;
                }
                else if (inode != null)
                    inode.Hide();
            }
        }

        public void InitColumnGroups(DBTable table, Node node)
        {
            table.ColumnGroups.ListChanged -= schemaChanged;
            if (ShowColumnGroup)
                table.ColumnGroups.ListChanged += schemaChanged;

            foreach (DBColumnGroup columnGroup in table.ColumnGroups)
            {
                var gnode = Find(columnGroup);
                if (ShowColumnGroup)
                {
                    gnode = InitColumnGroup(columnGroup);
                    gnode.Group = node;
                }
                else if (gnode != null)
                    gnode.Hide();
            }
        }

        public Node InitColumnGroup(DBColumnGroup cgroup)
        {
            var node = InitItem(cgroup);
            var columns = cgroup.GetColumns();
            foreach (var column in columns)
            {
                var cnode = Find(column);
                if (ShowColumn && column.Access.View)
                {
                    if (cnode == null)
                        cnode = InitColumn(column);
                    else
                        cnode.Visible = true;
                    cnode.Group = node;
                }
                else if (cnode != null)
                    cnode.Hide();
            }
            return node;
        }

        public void InitConstraints(DBTable table, Node node)
        {
            table.Constraints.ListChanged -= schemaChanged;

            if (ShowConstraint)
                table.Constraints.ListChanged += schemaChanged;

            foreach (var constr in table.Constraints)
            {
                var inode = Find(constr);
                if (ShowConstraint)
                {
                    if (inode == null)
                        inode = InitItem(constr);
                    else
                        inode.Visible = true;
                    inode.Group = node;
                }
                else if (inode != null)
                    inode.Hide();
            }
        }

        public void InitForeigns(DBTable table, Node node)
        {
            table.Foreigns.ListChanged -= schemaChanged;
            if (ShowForeign)
                table.Foreigns.ListChanged += schemaChanged;

            foreach (var constr in table.Foreigns)
            {
                var inode = Find(constr);
                if (ShowForeign)
                {
                    if (inode == null)
                        inode = InitItem(constr);
                    else
                        inode.Visible = true;
                    inode.Group = node;
                }
                else if (inode != null)
                    inode.Hide();
            }
        }

        public Node InitColumn(DBColumn column)
        {
            var node = InitItem(column);
            return node;
        }

        private Node InitRelation(DBForeignKey relation)
        {
            Node node = InitItem(relation);
            return node;
        }

        public Node InitItem(object obj)
        {
            return InitItem(obj, GetName(obj));
        }

        public Node InitItem(object obj, string name)
        {
            Node node = Find(name);
            if (node == null)
                node = new Node();
            node.Glyph = Common.Locale.GetGlyph(obj.GetType().FullName, obj.GetType().Name);
            if (node.Glyph == GlyphType.None)
            {
                if (obj is DBSchema)
                    node.Glyph = GlyphType.Database;
                if (obj is DBTable)
                    node.Glyph = GlyphType.Table;
                if (obj is DBColumn)
                    node.Glyph = GlyphType.Columns;
            }
            node.Visible = true;
            node.Tag = obj;
            node.Name = name;
            node.Text = obj.ToString();
            return node;
        }
    }
}

