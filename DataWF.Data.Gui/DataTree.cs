﻿using DataWF.Common;
using DataWF.Gui;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Data.Gui
{
    [Flags]
    public enum DataTreeKeys
    {
        None = 0,
        Schema = 1 << 0,
        TableGroup = 1 << 2,
        Table = 1 << 3,
        ColumnGroup = 1 << 4,
        Column = 1 << 5,
        Index = 1 << 6,
        Constraint = 1 << 7,
        Foreign = 1 << 8,
        Procedure = 1 << 9,
        ProcedureParam = 1 << 10,
        Sequence = 1 << 11,
        CheckTableView = 1 << 12,
        CheckTableAdmin = 1 << 13,
        LogTable = 1 << 14
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
        private DBSchemaItem datafilter = null;
        private DataTreeKeys dataKeys = DataTreeKeys.None;
        private TextEntry filterEntry;

        //TODO
        //private bool checkDelete = false;

        public DataTree() : base()
        {
            ListSensetive = true;
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

        [DefaultValue(true)]
        public bool ShowSequences
        {
            get { return (dataKeys & DataTreeKeys.Sequence) == DataTreeKeys.Sequence; }
        }

        public void OnItemListChanged(object sender, PropertyChangedEventArgs arg)
        {
            var newItem = (DBSchemaItem)sender;
            var node = Init(newItem);

            Node onode = newItem is DBTableItem tableItem ? Find(tableItem.Table) : Find(newItem.Schema);
            if (newItem is DBTableGroup tableGroup && tableGroup.Group != null && ShowTableGroup)
            {
                onode = Find(((DBTableGroup)newItem).Group);
            }
            else if (newItem is DBTable table && table.Group != null && ShowTableGroup)
            {
                onode = Find(((DBTable)newItem).Group);
            }
            else if (newItem is DBColumn column && column.Group != null && ShowColumnGroup)
            {
                onode = Find(((DBColumn)newItem).Group);
            }

            if (onode != null)
            {
                node.Group = onode;
            }
            else
            {
                Nodes.Add(node);
            }
        }

        public void OnItemsListChanged(object sender, EventArgs e)
        {
            if (e is NotifyCollectionChangedEventArgs cArgs)
            {
                if ((!ShowTableGroup && sender is DBTableGroupList)
                    || (!ShowTable && sender is DBTableList)
                    || (!ShowProcedures && sender is DBProcedureList)
                    || (!ShowSequences && sender is DBSequenceList)
                    || (!ShowColumn && sender is DBColumnList<DBColumn>)
                    || (!ShowColumnGroup && sender is DBColumnGroupList))
                    return;

                var newItem = cArgs.NewItems?.Cast<DBSchemaItem>().FirstOrDefault();
                var oldItem = cArgs.OldItems?.Cast<DBSchemaItem>().FirstOrDefault();

                switch (cArgs.Action)
                {

                    case NotifyCollectionChangedAction.Reset:
                        RefreshData();
                        break;
                    case NotifyCollectionChangedAction.Add:
                        OnItemListChanged(newItem, new PropertyChangedEventArgs(string.Empty));
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        Remove(oldItem);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        Remove(oldItem);
                        OnItemListChanged(newItem, new PropertyChangedEventArgs(string.Empty));
                        break;

                }
            }
            else if (e is PropertyChangedEventArgs pArgs)
            {
                OnItemListChanged(sender, pArgs);
            }
        }
        public void Remove(DBSchemaItem oldItem)
        {
            var node = Find(oldItem);
            if (node != null)
            {
                Nodes.Remove(node);
            }
        }
        public DBSchemaItem SelectedDBItem
        {
            get { return (SelectedNode as SchemaItemNode)?.Item; }
            set { SelectedNode = value == null ? null : Find(value); }
        }

        public DBSchema CurrentSchema
        {
            get
            {
                return SelectedDBItem == null ? null : SelectedDBItem is DBSchema
                    ? (DBSchema)SelectedDBItem : SelectedDBItem.Schema;
            }
        }

        public DBTable CurrentTable
        {
            get
            {
                return SelectedDBItem == null ? null : SelectedDBItem is DBTable
                    ? (DBTable)SelectedDBItem : (SelectedDBItem as IDBTableContent).Table;
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
            DBService.Schems.ItemsListChanged -= OnItemsListChanged;
            if (ShowSchema)
                DBService.Schems.ItemsListChanged += OnItemsListChanged;

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
            else if (item is DBProcedure)
                node = InitProcedure((DBProcedure)item);
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

            InitList(schema.TableGroups.GetTopParents(), node, ShowTableGroup);


            foreach (var table in schema.Tables)
            {
                if (table.Group != null && ShowTableGroup)
                    continue;
                CheckItem(table, node, ShowTable
                    && (!CheckTableView || (CheckTableView && table.Access.GetFlag(AccessType.Read, GuiEnvironment.User)))
                    && (!CheckTableAdmin || (CheckTableAdmin && table.Access.GetFlag(AccessType.Admin, GuiEnvironment.User)))
                    && (!(table is IDBLogTable) || ShowLogTable));
            }

            InitList(schema.Procedures.SelectByParent(null), node, ShowProcedures, "Procedures");
            InitList(schema.Sequences, node, ShowSequences, "Sequences");

            return node;
        }

        public void InitList<T>(IEnumerable<T> list, SchemaItemNode node, bool show, string nodeName = null) where T : DBSchemaItem
        {
            if (!string.IsNullOrEmpty(nodeName))
            {
                var subNode = Find(node.Name + nodeName);
                if (show)
                {
                    if (subNode == null)
                    {
                        subNode = new SchemaItemNode() { Name = node.Name + nodeName, Text = nodeName };
                    }
                    subNode.Group = node;
                    node = subNode;
                }
                else
                {
                    subNode?.Hide();
                    return;
                }
            }
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
                    && (!CheckTableView || (CheckTableView && table.Access.GetFlag(AccessType.Read, GuiEnvironment.User)))
                    && (!CheckTableAdmin || (CheckTableAdmin && table.Access.GetFlag(AccessType.Admin, GuiEnvironment.User)))
                    && (!(table is IDBLogTable) || ShowLogTable));
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

            foreach (DBColumnGroup columnGroup in table.ColumnGroups)
            {
                CheckItem(columnGroup, node, ShowColumnGroup);
            }
        }

        public void InitColumns(SchemaItemNode node)
        {
            var table = (DBTable)node.Item;

            foreach (DBColumn column in table.Columns)
            {
                if (column.Group != null && ShowColumnGroup)
                    continue;
                CheckItem(column, node, ShowColumn && column.Access.GetFlag(AccessType.Read, GuiEnvironment.User));
            }
        }

        public void InitIndexes(SchemaItemNode node)
        {
            var table = (DBTable)node.Item;

            foreach (var index in table.Indexes)
            {
                CheckItem(index, node, ShowIndex);
            }
        }

        public void InitConstraints(SchemaItemNode node)
        {
            var table = (DBTable)node.Item;

            foreach (var constr in table.Constraints)
            {
                CheckItem(constr, node, ShowConstraint);
            }
        }

        public void InitForeigns(SchemaItemNode node)
        {
            var table = (DBTable)node.Item;

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
                CheckItem(column, node, ShowColumn && column.Access.GetFlag(AccessType.Read, GuiEnvironment.User));
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
            node.Glyph = Locale.GetGlyph(item.GetType(), item.GetType().Name);
            if (node.Glyph == GlyphType.None)
            {
                if (item is DBSchema)
                {
                    node.Glyph = GlyphType.Database;
                    node.GlyphColor = Colors.LightSteelBlue;
                }
                else if (item is DBTableGroup)
                {
                    node.Glyph = GlyphType.FolderOTable;
                    node.GlyphColor = Colors.BurlyWood;
                }
                else if (item is DBTable)
                {
                    node.Glyph = GlyphType.Table;
                    node.GlyphColor = Colors.LightSteelBlue;
                }
                else if (item is DBColumnGroup)
                {
                    node.Glyph = GlyphType.Tags;
                    node.GlyphColor = Colors.BurlyWood;
                }
                else if (item is DBColumn)
                {
                    node.Glyph = GlyphType.Tag;
                    node.GlyphColor = Colors.ForestGreen;
                }
                else if (item is DBIndex)
                {
                    node.Glyph = GlyphType.MapMarker;
                    node.GlyphColor = Colors.Purple;
                }
                else if (item is DBForeignKey)
                {
                    node.Glyph = GlyphType.Link;
                    node.GlyphColor = Colors.Orange;
                }
                else if (item is DBConstraint)
                {
                    node.Glyph = GlyphType.Key;
                    node.GlyphColor = Colors.YellowGreen;
                }
                else if (item is DBSequence)
                {
                    node.Glyph = GlyphType.Plus;
                }
            }
            node.Visible = true;
            node.Localize();
            return node;
        }

        public virtual string GetName(object obj)
        {
            string str = "";
            if (obj != null)
                str = Locale.GetTypeCategory(obj.GetType()) + obj.GetHashCode();

            return str;
        }

        public TextEntry FilterEntry
        {
            get { return filterEntry; }
            set
            {
                if (filterEntry != null)
                    filterEntry.Changed -= FilterEntryChanged;

                filterEntry = value;

                if (filterEntry != null)
                    filterEntry.Changed += FilterEntryChanged;
            }
        }

        private void FilterEntryChanged(object sender, EventArgs e)
        {
            var entry = (TextEntry)sender;
            var list = listSource as SelectableListView<Node>;
            list.FilterQuery.Parameters.Clear();

            if (entry.Text?.Length != 0)
            {
                TreeMode = false;
                list.FilterQuery.Parameters.Add(LogicType.And, LayoutNodeList<Node>.FullPathInvoker, CompareType.Like, entry.Text);
            }
            else
            {
                TreeMode = true;
            }
            list.UpdateFilter();
        }

        protected override void Dispose(bool disposing)
        {
            DBService.Schems.ItemsListChanged -= OnItemsListChanged;
            base.Dispose(disposing);
        }
    }
}

