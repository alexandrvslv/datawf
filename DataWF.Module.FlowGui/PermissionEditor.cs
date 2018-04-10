using DataWF.Common;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Module.Common;
using DataWF.Module.CommonGui;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Module.FlowGui
{
    public class PermissionEditor : VPanel, IDockContent
    {
        private UserGroup userGroup;
        private ToolItem toolSave;
        private ToolItem toolExport;
        private ToolItem toolReject;
        private ToolItem toolLogs;
        private ToolTextEntry toolFilterText;
        private Toolsbar bar;
        private LayoutList list;
        private FlowTree tree;
        private GroupBox map;
        private List<DBItem> changes = new List<DBItem>();

        public PermissionEditor()
        {
            toolLogs = new ToolItem(ToolLogsClick) { Name = "Logs", Glyph = GlyphType.History };
            toolSave = new ToolItem(ToolSaveClick) { Name = "Save", Sensitive = false, Glyph = GlyphType.SaveAlias };
            toolReject = new ToolItem(ToolCancelClick) { Name = "Cancel", Glyph = GlyphType.Undo };
            toolExport = new ToolItem(ToolExportClick) { Name = "Export", Glyph = GlyphType.FileTextO };
            toolFilterText = new ToolTextEntry { Name = "FilterText" };

            list = new LayoutList()
            {
                EditMode = EditModes.ByClick,
                EditState = EditListState.Edit,
                GenerateColumns = false,
                GenerateToString = false,
                Grouping = false,
                Mode = LayoutListMode.List,
                Name = "list",
                Text = "columns"
            };

            bar = new Toolsbar(
                toolSave,
                toolReject,
                toolExport,
                toolLogs,
                new ToolSeparator { FillWidth = true },
                toolFilterText)
            { Name = "Bar" };

            var listInfo = new LayoutListInfo(
                new LayoutColumn
                {
                    Name = nameof(Node.ToString),
                    Editable = false,
                    Width = 120,
                    FillWidth = true,
                    Invoker = new ToStringInvoker()
                },
                new LayoutColumn
                {
                    Name = "View",
                    Width = 60,
                    Invoker = new Invoker<TableItemNode, bool?>("View",
                                                            (item) => item.Access?.Get(Group).View,
                                                            (item, value) =>
                                                            {
                                                                if (item.Access == null)
                                                                    return;
                                                                if (item.Access.View || item.Access.Admin)
                                                                {
                                                                    var access = item.Access.Get(Group);
                                                                    access.View = value.Value;
                                                                    item.Access.Add(access);
                                                                    CheckSave(item.Item);
                                                                }
                                                            })
                },
                new LayoutColumn
                {
                    Name = "Edit",
                    Width = 60,
                    Invoker = new Invoker<TableItemNode, bool?>("Edit",
                                                            (item) => item.Access?.Get(Group).Edit,
                                                            (item, value) =>
                                                            {
                                                                if (item.Access == null)
                                                                    return;
                                                                if (item.Access.Edit || item.Access.Admin)
                                                                {
                                                                    var access = item.Access.Get(Group);
                                                                    access.Edit = value.Value;
                                                                    item.Access.Add(access);
                                                                    CheckSave(item.Item);
                                                                }
                                                            })
                },
                new LayoutColumn
                {
                    Name = "Create",
                    Width = 60,
                    Invoker = new Invoker<TableItemNode, bool?>("Create",
                                                            (item) => item.Access?.Get(Group).Create,
                                                            (item, value) =>
                                                            {
                                                                if (item.Access == null)
                                                                    return;
                                                                if (item.Access.Create || item.Access.Admin)
                                                                {
                                                                    var access = item.Access.Get(Group);
                                                                    access.Create = value.Value;
                                                                    item.Access.Add(access);
                                                                    CheckSave(item.Item);
                                                                }
                                                            })
                },
                new LayoutColumn
                {
                    Name = "Delete",
                    Width = 60,
                    Invoker = new Invoker<TableItemNode, bool?>("Delete",
                                                            (item) => item.Access?.Get(Group).Delete,
                                                            (item, value) =>
                                                            {
                                                                if (item.Access == null)
                                                                    return;
                                                                if (item.Access.Delete || item.Access.Admin)
                                                                {
                                                                    var access = item.Access.Get(Group);
                                                                    access.Delete = value.Value;
                                                                    item.Access.Add(access);
                                                                    CheckSave(item.Item);
                                                                }
                                                            })
                },
                new LayoutColumn
                {
                    Name = "Admin",
                    Width = 60,
                    Invoker = new Invoker<TableItemNode, bool?>("Admin",
                                                            (item) => item.Access?.Get(Group).Admin,
                                                            (item, value) =>
                                                            {
                                                                if (item.Access == null)
                                                                    return;
                                                                if (item.Access.Admin || item.Access.Admin)
                                                                {
                                                                    var access = item.Access.Get(Group);
                                                                    access.Admin = value.Value;
                                                                    item.Access.Add(access);
                                                                    CheckSave(item.Item);
                                                                }
                                                            })
                },
                new LayoutColumn
                {
                    Name = "Accept",
                    Width = 60,
                    Invoker = new Invoker<TableItemNode, bool?>("Accept",
                                                            (item) => item.Access?.Get(Group).Accept,
                                                            (item, value) =>
                                                            {
                                                                if (item.Access == null)
                                                                    return;
                                                                if (item.Access.Accept || item.Access.Admin)
                                                                {
                                                                    var access = item.Access.Get(Group);
                                                                    access.Accept = value.Value;
                                                                    item.Access.Add(access);
                                                                    CheckSave(item.Item);
                                                                }
                                                            })
                },
                new LayoutColumn
                {
                    Name = "Table",
                    Visible = false,
                    Invoker = new PropertyInvoker<TableItemNode, string>("TableName")
                })
            {
                Tree = true,
                ColumnsVisible = true,
                HeaderVisible = true,
                GroupCount = false
            };
            listInfo.Sorters.Add(new LayoutSort("Order"));
            var nodeInfo = new LayoutNodeInfo { Columns = listInfo };
            tree = new FlowTree
            {
                NodeInfo = nodeInfo,
                Name = "tree",
                EditState = EditListState.Edit,
                EditMode = EditModes.ByF2,
                UserKeys = UserTreeKeys.Department | UserTreeKeys.User | UserTreeKeys.Group | UserTreeKeys.Permission,
                FlowKeys = FlowTreeKeys.Template | FlowTreeKeys.TemplateParam | FlowTreeKeys.Work | FlowTreeKeys.Stage | FlowTreeKeys.StageParam,
                FilterEntry = toolFilterText.Entry
            };
            tree.SelectionChanged += TreeSelectionChanged;

            map = new GroupBox(
                new GroupBoxItem
                {
                    Text = "Params",
                    Widget = list,
                    FillWidth = true
                },
                new GroupBoxItem
                {
                    Text = "Permissions",
                    Widget = tree,
                    FillWidth = true,
                    FillHeight = true,
                    Row = 1
                });

            Name = "PermissionEditor";
            Text = "Permissions";
            PackStart(bar, false, false);
            PackStart(map, true, true);

            UserLog.RowLoged += FlowEnvirRowLoged;

            Localize();
        }

        private void FlowEnvirRowLoged(object sender, DBItemEventArgs e)
        {
            if (e.Item == userGroup && changes.Count > 0)
            {
                e.Transaction.Tag = sender;
                foreach (var item in changes)
                    if (item != userGroup)
                        item.Save(e.Transaction);
                changes.Clear();
            }
        }

        public void CheckSave(IDBTableContent item)
        {
            var dbItem = item as DBItem;
            if (dbItem == null)
                return;
            dbItem.Access = dbItem.Access;
            if (dbItem.IsChanged)
            {
                if (!changes.Contains(dbItem))
                    changes.Add(dbItem);
            }
            else
            {
                changes.Remove(dbItem);
            }
            CheckSave();
        }

        private void CheckSave()
        {
            toolSave.Sensitive = userGroup.IsChanged || changes.Count > 0;
        }

        public static string GetName(UserGroup p)
        {
            return "Group" + p.Id;
        }

        private void TreeSelectionChanged(object sender, EventArgs e)
        {
        }

        public UserGroup Group
        {
            get { return userGroup; }
            set
            {
                if (Group == value)
                    return;
                if (userGroup != null)
                    userGroup.PropertyChanged -= GroupPropertyChanged;

                if (value.Id == null)
                    value.GenerateId();
                userGroup = value;

                if (userGroup != null)
                    userGroup.PropertyChanged += GroupPropertyChanged;

                list.FieldSource = Group;

                //tree.RefreshData();

                foreach (var node in tree.Nodes.GetTopLevel())
                {
                    node.Expand = true;
                }
                Name = GetName(userGroup);
                Text = userGroup.ToString();
            }
        }

        private void GroupPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CheckSave();
            Text = userGroup.ToString();

        }

        private void ToolExportClick(object sender, EventArgs e)
        {
            if (tree.SelectedNode != null)
            {
                DataQuery query = new DataQuery();
                query.Query = tree.GenereteExport();
                query.ShowDialog(this);
            }
        }

        private void ToolLogsClick(object sender, EventArgs e)
        {
            var logs = new DataLogView();
            logs.SetFilter(userGroup);
            logs.ShowWindow(this);
        }

        private void ToolSaveClick(object sender, EventArgs e)
        {
            if (changes.Count > 0)
            {
                userGroup.Status = DBStatus.Edit;
                userGroup.Stamp = DateTime.Now;
                userGroup.Save();
            }
            userGroup.Save();
        }

        private void ToolCancelClick(object sender, EventArgs e)
        {
            userGroup.Reject();
            if (changes.Count > 0)
            {
                userGroup.Status = DBStatus.Edit;
                foreach (var item in changes)
                    item.Reject();
                changes.Clear();
            }
            //tree.Refresh();
        }

        public void Localize()
        {
            bar.Localize();
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
            userGroup.PropertyChanged -= GroupPropertyChanged;
            base.Dispose(disposing);
        }

    }

}
