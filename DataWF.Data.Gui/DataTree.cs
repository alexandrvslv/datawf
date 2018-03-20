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
        CheckTableAdmin = 2048,
        LogTable = 4096
    }

    public class SchemaItemNode : Node, ILocalizable
    {
        public DBSchemaItem Item { get; set; }

        public void Localize()
        {
            if (Item != null)
            {
                Text = Item.ToString();
            }
        }
    }

    public class DataTree : LayoutList
    {
        private ListChangedEventHandler schemaChanged;
        private DBSchemaItem datafilter = null;
        DataTreeKeys dataKeys = DataTreeKeys.None;
        //TODO
        //private bool checkDelete = false;

        public DataTree() : base()
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

        protected virtual void RefreshData()
        {
            if (datafilter != null)
            {
                Nodes.Add(Init(datafilter));
            }
            else
            {
                InitSchems();
            }
        }

        public DBSchemaItem DataFilter
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

        [DefaultValue(false)]
        public bool ShowLogTable
        {
            get { return (dataKeys & DataTreeKeys.LogTable) == DataTreeKeys.LogTable; }
        }

        [DefaultValue(true)]
        public bool CheckTableView
        {
            get { return (dataKeys & DataTreeKeys.CheckTableView) == DataTreeKeys.CheckTableView; }
        }

        [DefaultValue(false)]
        public bool CheckTableAdmin
        {
            get { return (dataKeys & DataTreeKeys.CheckTableAdmin) == DataTreeKeys.CheckTableAdmin; }
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
                var node = Init(item);
                if (arg.ListChangedType != ListChangedType.ItemDeleted)
                {
                    Node onode = item is DBTableItem ? Find(((DBTableItem)item).Table) : Find(item.Schema);
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

        public DBSchemaItem SelectedDBItem
        {
            get { return (SelectedNode as SchemaItemNode)?.Item; }
        }

        public DBSchema CurrentSchema
        {
            get
            {
                return SelectedDBItem == null ? null : SelectedDBItem is DBSchema
                    ? (DBSchema)SelectedDBItem : SelectedDBItem.Schema;
            }
        }

        public SchemaItemNode Find(object obj)
        {
            return obj == null ? null : Find(GetName(obj));
        }

        public SchemaItemNode Find(string name)
        {
            return Nodes.SelectOne("Name", CompareType.Equal, name) as SchemaItemNode;
        }

        private void InitSchems()
        {
            DBService.Schems.ListChanged -= schemaChanged;
            if (ShowSchema)
                DBService.Schems.ListChanged += schemaChanged;

            Nodes.Sense = false;
            foreach (var schema in DBService.Schems)
            {
                CheckItem(schema, null, ShowSchema);
            }
            Nodes.Sense = true;
        }

        public virtual SchemaItemNode Init(DBSchemaItem item)
        {
            SchemaItemNode node = null;
            if (item is DBSchema)
                node = InitSchema((DBSchema)item);
            else if (item is DBTableGroup)
                node = InitTableGroup((DBTableGroup)item);
            else if (item is DBTable)
                node = InitTable((DBTable)item);
            else if (item is DBColumnGroup)
                node = InitColumnGroup((DBColumnGroup)item);
            else if (item is DBColumn)
                node = InitColumn((DBColumn)item);
            else
                node = InitItem(item);

            return node;
        }

        public virtual SchemaItemNode CheckItem(DBSchemaItem item, SchemaItemNode group, bool show)
        {
            SchemaItemNode node = null;
            if (show)
            {
                node = Init(item);
                if (group == null)
                {
                    Nodes.Add(node);
                }
                else
                {
                    node.Group = group;
                }
            }
            else
            {
                node = Find(item);
                node?.Hide();
            }
            return node;
        }


        public SchemaItemNode InitSchema(DBSchema schema)
        {
            var node = InitItem(schema);

            schema.TableGroups.ListChanged -= schemaChanged;
            schema.Tables.ListChanged -= schemaChanged;
            schema.Procedures.ListChanged -= schemaChanged;

            if (ShowTableGroup)
                schema.TableGroups.ListChanged += schemaChanged;
            if (ShowTable)
                schema.Tables.ListChanged += schemaChanged;
            if (ShowProcedures)
                schema.Procedures.ListChanged += schemaChanged;

            foreach (var tgroup in schema.TableGroups.GetTopParents())
            {
                CheckItem(tgroup, node, ShowTableGroup);
            }

            foreach (var table in schema.Tables)
            {
                if (table.Group != null && ShowTableGroup)
                    continue;
                CheckItem(table, node, ShowTable
                    && (!CheckTableView || (CheckTableView && table.Access.View))
                    && (!CheckTableAdmin || (CheckTableAdmin && table.Access.Admin))
                    && (!(table is DBLogTable) || ShowLogTable));
            }

            foreach (var procedure in schema.Procedures.SelectByParent(null))
            {
                CheckItem(procedure, node, ShowProcedures);
            }
            return node;
        }

        public void InitList<T>(IEnumerable<T> list, SchemaItemNode node, bool show) where T : DBSchemaItem
        {
            foreach (var item in list)
            {
                CheckItem(item, node, show);
            }
        }

        public SchemaItemNode InitProcedure(DBProcedure procedure)
        {
            var node = InitItem(procedure);
            InitList(procedure.Parameters, node, ShowProcedureParam);
            foreach (var sprocedure in procedure.Childs)
            {
                InitProcedure(sprocedure).Group = node;
            }
            return node;
        }

        public SchemaItemNode InitTableGroup(DBTableGroup tgroup)
        {
            var node = InitItem(tgroup);

            foreach (var item in tgroup.GetChilds())
            {
                CheckItem(item, node, ShowTableGroup);
            }

            foreach (var table in tgroup.GetTables())
            {
                CheckItem(table, node, ShowTable
                    && (!CheckTableView || (CheckTableView && table.Access.View))
                    && (!CheckTableAdmin || (CheckTableAdmin && table.Access.Admin))
                    && (!(table is DBLogTable) || ShowLogTable));
            }
            return node;
        }


        public SchemaItemNode InitTable(DBTable table)
        {
            var node = InitItem(table);
            InitColumnGroups(node);
            InitColumns(node);
            InitIndexes(node);
            InitConstraints(node);
            InitForeigns(node);
            return node;
        }

        public void InitColumnGroups(SchemaItemNode node)
        {
            var table = (DBTable)node.Item;
            table.ColumnGroups.ListChanged -= schemaChanged;
            if (ShowColumnGroup)
                table.ColumnGroups.ListChanged += schemaChanged;

            foreach (DBColumnGroup columnGroup in table.ColumnGroups)
            {
                CheckItem(columnGroup, node, ShowColumnGroup);
            }
        }

        public void InitColumns(SchemaItemNode node)
        {
            var table = (DBTable)node.Item;
            table.Columns.ListChanged -= schemaChanged;
            if (ShowColumn)
                table.Columns.ListChanged += schemaChanged;

            foreach (DBColumn column in table.Columns)
            {
                if (column.Group != null && ShowColumnGroup)
                    continue;
                CheckItem(column, node, ShowColumn && column.Access.View);
            }
        }

        public void InitIndexes(SchemaItemNode node)
        {
            var table = (DBTable)node.Item;
            table.Indexes.ListChanged -= schemaChanged;
            if (ShowIndex)
                table.Indexes.ListChanged += schemaChanged;

            foreach (var index in table.Indexes)
            {
                CheckItem(index, node, ShowIndex);
            }
        }

        public void InitConstraints(SchemaItemNode node)
        {
            var table = (DBTable)node.Item;
            table.Constraints.ListChanged -= schemaChanged;
            if (ShowConstraint)
                table.Constraints.ListChanged += schemaChanged;

            foreach (var constr in table.Constraints)
            {
                CheckItem(constr, node, ShowConstraint);
            }
        }

        public void InitForeigns(SchemaItemNode node)
        {
            var table = (DBTable)node.Item;
            table.Foreigns.ListChanged -= schemaChanged;
            if (ShowForeign)
                table.Foreigns.ListChanged += schemaChanged;

            foreach (var constr in table.Foreigns)
            {
                CheckItem(constr, node, ShowForeign);
            }
        }

        public SchemaItemNode InitColumnGroup(DBColumnGroup cgroup)
        {
            var node = InitItem(cgroup);
            foreach (var column in cgroup.GetColumns())
            {
                CheckItem(column, node, ShowColumn && column.Access.View);
            }
            return node;
        }

        public SchemaItemNode InitColumn(DBColumn column)
        {
            return InitItem(column);
        }

        private SchemaItemNode InitRelation(DBForeignKey relation)
        {
            return InitItem(relation);
        }

        public SchemaItemNode InitItem(DBSchemaItem item)
        {
            string name = GetName(item);
            var node = Find(name);
            if (node == null)
            {
                node = new SchemaItemNode { Item = item, Name = name };
            }
            node.Glyph = Locale.GetGlyph(item.GetType().FullName, item.GetType().Name);
            if (node.Glyph == GlyphType.None)
            {
                if (item is DBSchema)
                    node.Glyph = GlyphType.Database;
                else if (item is DBTableGroup)
                    node.Glyph = GlyphType.FolderOTable;
                else if (item is DBTable)
                    node.Glyph = GlyphType.Table;
                else if (item is DBColumnGroup)
                    node.Glyph = GlyphType.FolderOColumn;
                else if (item is DBColumn)
                    node.Glyph = GlyphType.Columns;
            }
            node.Visible = true;
            node.Localize();
            return node;
        }

        public virtual string GetName(object obj)
        {
            string str = "";
            if (obj != null)
                str = obj.GetType().FullName + obj.GetHashCode();

            return str;
        }
    }
}

