using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt.Drawing;
using Xwt;

namespace DataWF.Module.FlowGui
{
    public class PermissionEditor : VPanel, IDockContent
    {
        private UserGroup _group;
        private ToolItem toolSave = new ToolItem();
        private ToolItem toolExport = new ToolItem();
        private ToolItem toolReject = new ToolItem();
        private ToolItem toolLogs = new ToolItem();
        private ToolTextEntry toolFilterText = new ToolTextEntry();
        private Toolsbar tools = new Toolsbar();
        private LayoutList list = new LayoutList();
        private FlowTree tree = new FlowTree();
        private GroupBox map = new GroupBox();
        private List<DBItem> changes = new List<DBItem>();

        public PermissionEditor()
        {
            PackStart(tools, false, false);
            PackStart(map, true, true);
            list.Text = "columns";
            //toolFilterText.Alignment = ToolStripItemAlignment.Right;

            map.Add(new GroupBoxItem()
            {
                Text = "Params",
                Widget = list,
                FillWidth = true
            });
            map.Add(new GroupBoxItem()
            {
                Text = "Permissions",
                Widget = tree,
                FillWidth = true,
                FillHeight = true,
                Row = 1
            });
            toolFilterText.Name = "toolFilterText";
            toolFilterText.TextChanged += this.ToolFilterTextChanged;

            toolLogs.Name = "toolLogs";
            toolLogs.Click += this.ToolLogsClick;

            toolSave.Name = "toolSave";
            toolSave.Click += this.ToolSaveClick;
            toolSave.Sensitive = false;

            toolReject.Name = "toolCancel";
            toolReject.Click += this.ToolCancelClick;

            toolExport.Name = "toolExport";
            toolExport.Click += this.ToolExportClick;

            list.EditMode = EditModes.ByClick;
            list.EditState = EditListState.Edit;
            list.FieldSource = null;
            this.list.GenerateColumns = false;
            this.list.GenerateToString = false;
            this.list.Grouping = false;
            this.list.ListSource = null;
            this.list.Mode = LayoutListMode.List;
            this.list.Name = "list";
            this.list.SelectedItem = null;
            // 
            // tools
            // 
            tools.Items.Add(toolSave);
            tools.Items.Add(toolReject);
            tools.Items.Add(toolExport);
            tools.Items.Add(toolLogs);
            tools.Items.Add(toolFilterText);
            tools.Name = "toolStrip2";
            // 
            // tree
            // 

            tree.Mode = LayoutListMode.Tree;
            tree.Name = "tree";
            tree.SelectionChanged += this.TreeSelectionChanged;
            tree.CellMouseClick += this.TreeCellMouseClick;
            tree.CellValueChanged += TreeCellValueChanged;
            tree.RetriveCellEditor += TreeRetriveCellEditor;
            tree.Nodes.ListChanged += TreeNodesListChanged;
            tree.EditState = EditListState.Edit;
            tree.EditMode = EditModes.ByF2;
            tree.ListInfo.ColumnsVisible = true;
            tree.ListInfo.HeaderVisible = true;
            tree.ListInfo.GroupCount = false;
            tree.ListInfo.Columns.Add("View", 60);
            tree.ListInfo.Columns.Add("Edit", 60);
            tree.ListInfo.Columns.Add("Create", 60);
            tree.ListInfo.Columns.Add("Delete", 60);
            tree.ListInfo.Columns.Add("Admin", 60);
            tree.ListInfo.Columns.Add("Accept", 60);
            tree.ListInfo.Columns.Add("Type").Visible = false;
            tree.ListInfo.Sorters.Clear();
            //tree.ListInfo.Sorting.Add(new PSort("Type", ListSortDirection.Ascending, true));//  (group, ListSortDirection.Ascending);
            tree.ListInfo.Sorters.Add(new LayoutSort("Order"));
            tree.RefreshInfo();

            this.Name = "PermissionEditor";
            this.Text = "Permissions";

            UserLog.RowLoged += FlowEnvirRowLoged;

            Localize();
        }

        private void FlowEnvirRowLoged(object sender, DBItemEventArgs e)
        {
            if (e.Item == _group && changes.Count > 0)
            {
                using (var transaction = new DBTransaction(e.Item.Table.Schema.Connection) { Tag = sender, Reference = false })
                {
                    foreach (var item in changes)
                        if (item != _group)
                            item.Save(transaction);
                    transaction.Commit();
                    changes.Clear();
                }
            }
        }

        private void CheckSave()
        {
            toolSave.Sensitive = _group.IsChanged || changes.Count > 0;
        }

        public static string GetName(UserGroup p)
        {
            return "Group" + p.Id;
        }

        private void ToolFilterTextChanged(object sender, EventArgs e)
        {
            if (toolFilterText.Text.Length != 0)
            {
                tree.TreeMode = false;
                tree.Nodes.DefaultView.FilterQuery.Parameters.Clear();
                QueryParameter p = new QueryParameter()
                {
                    Type = typeof(Node),
                    Property = "Header",
                    Comparer = CompareType.Like,
                    Value = toolFilterText.Text
                };
                tree.Nodes.DefaultView.FilterQuery.Parameters.Add(p);
            }
            else
            {
                tree.TreeMode = true;
            }
            tree.Nodes.DefaultView.UpdateFilter();
        }

        private void TreeCellValueChanged(object sender, LayoutValueChangedEventArgs e)
        {
            if (e == null || e.ListItem == null)
                return;
            var node = (Node)e.ListItem;

            if (node.Tag is DBItem)
            {
                var item = (DBItem)node.Tag;
                SetAccess(node, e.Cell.Name, (bool)e.Data);
                if (item is GroupPermission)
                {
                    var perm = (GroupPermission)item;
                    if (perm.Permission is DBTable)
                    {
                        foreach (var n in node.Childs)
                            SetAccess(n, e.Cell.Name, (bool)e.Data);

                    }
                }
            }
            else if (node.Tag is IDBTableView)
            {
                foreach (var n in node.Childs)
                    SetAccess(n, e.Cell.Name, (bool)e.Data);
            }
        }

        private void SetAccess(Node node, string name, bool flag)
        {
            var item = node.Tag as DBItem;
            var access = item.Access;
            var aitem = access.Get(Group);
            if (name == "View")
            {
                if (item != User.CurrentUser && (access.View || access.Admin))
                    aitem.View = flag;
                else
                {
                    node[name] = !flag;
                    MessageDialog.ShowMessage(ParentWindow, string.Format("View Access {0} denied for {1}", access, item));
                    return;
                }
            }
            else if (name == "Edit")
            {
                if (item != User.CurrentUser && (access.Edit || access.Admin))
                    aitem.Edit = flag;
                else
                {
                    node[name] = !flag;
                    MessageDialog.ShowMessage(ParentWindow, string.Format("Edit Access {0} denied for {1}", access, item));
                    return;
                }
            }
            else if (name == "Create")
            {
                if (item != User.CurrentUser && (access.Create || access.Admin))
                    aitem.Create = flag;
                else
                {
                    node[name] = !flag;
                    MessageDialog.ShowMessage(ParentWindow, string.Format("Create Access {0} denied for {1}", access, item));
                    return;
                }
            }
            else if (name == "Delete")
            {
                if (item != User.CurrentUser && (access.Delete || access.Admin))
                    aitem.Delete = flag;
                else
                {
                    node[name] = !flag;
                    MessageDialog.ShowMessage(ParentWindow, string.Format("Delete Access {0} denied for {1}", access, item));
                    return;
                }
            }
            else if (name == "Admin")
            {
                if (item != User.CurrentUser && access.Admin)
                    aitem.Admin = flag;
                else
                {
                    node[name] = !flag;
                    MessageDialog.ShowMessage(ParentWindow, string.Format("Admin Access {0} denied for {1}", access, item));
                    return;
                }
            }
            else if (name == "Accept")
            {
                if (item != User.CurrentUser && (access.Accept || access.Admin))
                    aitem.Accept = flag;
                else
                {
                    node[name] = !flag;
                    MessageDialog.ShowMessage(ParentWindow, string.Format("Accept Access {0} denied for {1}", access, item));
                    return;
                }
            }
            node[name] = flag;

            access.Add(aitem);
            item.Access = access;
            item.Attach();
            if (item.IsChanged)
            {
                if (!changes.Contains(item))
                    changes.Add(item);
            }
            else
                changes.Remove(item);
            CheckSave();
        }

        private ILayoutCellEditor TreeRetriveCellEditor(object listItem, object value, ILayoutCell cell)
        {
            if (cell.CellEditor == null)
            {
                if (cell.Name == "View" ||
                    cell.Name == "Edit" ||
                    cell.Name == "Create" ||
                    cell.Name == "Delete" ||
                    cell.Name == "Admin" ||
                    cell.Name == "Accept")
                    cell.CellEditor = new CellEditorCheck();
            }
            return cell.CellEditor;
        }

        private void TreeNodesListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemAdded)
            {
                Node node = tree.Nodes[e.NewIndex];
                if (node.Tag is GroupPermission || node.Tag is GroupPermissionList)
                    node["Type"] = "DataBase";
                else if (node.Tag is Work || node.Tag is WorkList || node.Tag is Stage)
                    node["Type"] = "Flows";
                else if (node.Tag is Template || node.Tag is TemplateList || node.Tag is TemplateParam || node.Tag is TemplateParamList)
                    node["Type"] = "Templates";
                else if (node.Tag is User || node.Tag is UserList)
                    node["Type"] = "User";
                else
                    node["Type"] = "Procedures";

                DBItem acceable = node.Tag as DBItem;
                if (acceable != null)
                {
                    AccessValue access = acceable.Access;
                    AccessItem permission = access.Get(Group);

                    node["View"] = permission.View;
                    node["Edit"] = permission.Edit;
                    node["Create"] = permission.Create;
                    node["Delete"] = permission.Delete;
                    node["Admin"] = permission.Admin;
                    node["Accept"] = permission.Accept;
                }
            }
        }

        private void TreeCellMouseClick(object sender, LayoutHitTestEventArgs e)
        {
        }

        private void TreeSelectionChanged(object sender, EventArgs e)
        {
        }

        public UserGroup Group
        {
            get { return _group; }
            set
            {
                if (Group == value)
                    return;
                if (_group != null)
                    _group.PropertyChanged -= GroupPropertyChanged;

                if (value.Id == null)
                    value.GenerateId();
                _group = value;

                if (_group != null)
                    _group.PropertyChanged += GroupPropertyChanged;

                list.FieldSource = Group;

                tree.FlowKeys = FlowTreeKeys.Permission | FlowTreeKeys.Stage | FlowTreeKeys.Work |
                    FlowTreeKeys.TemplateParam | FlowTreeKeys.Template |
                    FlowTreeKeys.User | FlowTreeKeys.Group;

                foreach (var node in tree.Nodes.GetTopLevel())
                {
                    node.Expand = true;
                }
                Name = GetName(_group);
                Text = _group.ToString();
            }
        }

        private void GroupPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CheckSave();
            Text = _group.ToString();

        }

        private void ToolExportClick(object sender, EventArgs e)
        {
            if (tree.SelectedNode != null)
            {
                DataQuery query = new DataQuery();
                query.Query = FlowExplorer.GenereteExport(tree);
                query.ShowDialog(this);
            }
        }

        private void ToolLogsClick(object sender, EventArgs e)
        {
            var logs = new DataLogView();
            logs.SetFilter(_group);
            logs.ShowWindow(this);
        }

        private void ToolSaveClick(object sender, EventArgs e)
        {
            if (changes.Count > 0)
            {
                _group.Status = DBStatus.Edit;
                _group.Stamp = DateTime.Now;
                _group.Save();
            }
            _group.Save();
        }

        private void ToolCancelClick(object sender, EventArgs e)
        {
            _group.Reject();
            if (changes.Count > 0)
            {
                _group.Status = DBStatus.Edit;
                foreach (var item in changes)
                    item.Reject();
                changes.Clear();
            }
            //tree.Refresh();
        }

        public void Localize()
        {
            GuiService.Localize(toolLogs, "PermissionEditor", "Logs", GlyphType.History);
            GuiService.Localize(toolSave, "PermissionEditor", "Save", GlyphType.SaveAlias);
            GuiService.Localize(toolReject, "PermissionEditor", "Reject", GlyphType.Undo);
            GuiService.Localize(toolExport, "PermissionEditor", "Export", GlyphType.FileTextO);
            GuiService.Localize(this, "PermissionEditor", "Permissions");

        }

        public DockType DockType
        {
            get { return DockType.Content; }
        }

        public bool HideOnClose
        {
            get { return false; }
        }

        //protected override void OnClosing(CancelEventArgs e)
        //{
        //    if (changes.Count > 0)
        //    {
        //        var result = MessageDialog.ShowMessage(this, Localize.Get("PermissionEditor", "You have unsaved data! Save?"), "On Close", MessageBoxButtons.YesNoCancel);
        //        if (result ==  Command.Cancel)
        //            e.Cancel = true;
        //        else if (result ==  Command.Yes)
        //            ToolSaveClick(this, e);
        //    }
        //    base.OnClosing(e);
        //}

        protected override void Dispose(bool disposing)
        {
            UserLog.RowLoged -= FlowEnvirRowLoged;
            _group.PropertyChanged -= GroupPropertyChanged;
            base.Dispose(disposing);
        }

    }

}
