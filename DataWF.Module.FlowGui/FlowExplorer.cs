using System;
using System.Collections;
using System.Text;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using DataWF.Gui;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Module.FlowGui
{
    [Module(true)]
    public class FlowExplorer : VPanel, IDockContent
    {
        private ToolWindow ose = new ToolWindow();
        private ListEditor se = new ListEditor();
        private Toolsbar barMain = new Toolsbar();
        private ToolItem toolMainCopy = new ToolItem();
        private ToolDropDown toolMainAdd = new ToolDropDown();
        private ToolItem toolMainRemove = new ToolItem();
        private ToolDropDown toolMainTools = new ToolDropDown();
        private ToolSearchEntry toolMainFilter = new ToolSearchEntry();
        private Menu contextAdd = new Menu();
        private GlyphMenuItem toolAddTemplate = new GlyphMenuItem();
        private GlyphMenuItem toolAddTemplateParam = new GlyphMenuItem();
        private GlyphMenuItem toolAddWork = new GlyphMenuItem();
        private GlyphMenuItem toolAddStage = new GlyphMenuItem();
        private GlyphMenuItem toolAddStageParam = new GlyphMenuItem();
        private GlyphMenuItem toolAddUser = new GlyphMenuItem();
        private GlyphMenuItem toolAddGroup = new GlyphMenuItem();
        private GlyphMenuItem toolAddScheduler = new GlyphMenuItem();
        private Menu contextTools = new Menu();
        private GlyphMenuItem toolRefresh = new GlyphMenuItem();
        private GlyphMenuItem toolGenerateDB = new GlyphMenuItem();
        private GlyphMenuItem toolConfig = new GlyphMenuItem();
        private GlyphMenuItem toolStat = new GlyphMenuItem();
        private FlowTree tree = new FlowTree();

        public FlowExplorer()
            : base()
        {
            PackStart(barMain, false, false);
            PackStart(tree, true, true);

            se.List.RetriveCellEditor += new PListGetEditorHandler(OptionsGetCellEditor);
            ose.Target = se;
            ose.ButtonAcceptClick += AcceptOnActivated;

            DBService.DBSchemaChanged += OnDBSchemaChanged;

            contextTools.Items.Add(toolRefresh);
            contextTools.Items.Add(toolGenerateDB);
            contextTools.Items.Add(toolConfig);
            contextTools.Items.Add(toolStat);

            toolRefresh.Name = "toolfresh";
            toolRefresh.Click += ToolMainRefreshClick;

            toolGenerateDB.Name = "toolGenerateFlowDB";
            toolGenerateDB.Click += ToolGenerateDBClick;

            toolStat.Name = "toolStat";
            toolStat.Click += ToolStatClick;

            //
            tree.SelectionChanged += TreeAfterSelect;
            tree.CellMouseClick += TreeNodeMouseClick;
            tree.CellDoubleClick += TreeNodeMouseDoubleClick;
            tree.Visible = true;
            tree.Status = DBStatus.Current;
            FlowTreeKeys keys = FlowTreeKeys.None;
            if (TemplateParam.DBTable.Access.View) keys |= FlowTreeKeys.TemplateParam;
            if (Template.DBTable.Access.View) keys |= FlowTreeKeys.Template;
            if (User.DBTable.Access.View) keys |= FlowTreeKeys.User;
            if (UserGroup.DBTable.Access.View) keys |= FlowTreeKeys.Group;
            if (Scheduler.DBTable.Access.View) keys |= FlowTreeKeys.Scheduler;
            if (StageParam.DBTable.Access.View) keys |= FlowTreeKeys.StageParam;
            if (Stage.DBTable.Access.View) keys |= FlowTreeKeys.Stage;
            if (Work.DBTable.Access.View) keys |= FlowTreeKeys.Work;
            tree.FlowKeys = keys;

            contextAdd.Items.Add(toolAddTemplate);
            contextAdd.Items.Add(toolAddTemplateParam);
            contextAdd.Items.Add(toolAddWork);
            contextAdd.Items.Add(toolAddStage);
            contextAdd.Items.Add(toolAddStageParam);
            contextAdd.Items.Add(toolAddUser);
            contextAdd.Items.Add(toolAddGroup);
            contextAdd.Items.Add(toolAddScheduler);

            toolAddTemplate.Sensitive = Template.DBTable.Access.Create;
            toolAddTemplate.Name = "toolAddTemplate";

            toolAddTemplateParam.Sensitive = TemplateParam.DBTable.Access.Create;
            toolAddTemplateParam.Name = "toolAddTemplateAttribute";

            toolAddWork.Name = "toolAddFlow";
            toolAddStage.Name = "toolAddFlowStage";

            toolAddGroup.Sensitive = UserGroup.DBTable.Access.Create;
            toolAddGroup.Name = "toolAddGroup";

            toolAddUser.Sensitive = User.DBTable.Access.Create;
            toolAddUser.Name = "toolAddUser";

            toolAddScheduler.Sensitive = Scheduler.DBTable.Access.Create;
            toolAddStageParam.Sensitive = StageParam.DBTable.Access.Create;
            toolAddStage.Sensitive = Stage.DBTable.Access.Create;
            toolAddWork.Sensitive = Work.DBTable.Access.Create;
            toolConfig.Sensitive = User.CurrentUser.Super.Value;

            barMain.Add(toolMainAdd);
            barMain.Add(toolMainRemove);
            barMain.Add(toolMainCopy);
            barMain.Add(toolMainTools);
            barMain.Add(toolMainFilter);

            toolMainFilter.Name = "toolFilterText";
            toolMainFilter.FillWidth = true;
            toolMainFilter.EntryTextChanged += ToolFilterTextChanged;

            toolMainCopy.Name = "toolMainCopy";
            toolMainCopy.Click += ToolMainCopyClick;

            toolMainAdd.Name = "toolMainAdd";
            toolMainAdd.ForeColor = Colors.DarkGreen;
            toolMainAdd.DropDown = contextAdd;

            toolMainRemove.Name = "toolMainRemove";
            toolMainRemove.ForeColor = Colors.DarkRed;
            toolMainRemove.Click += ToolMainRemoveClick;

            toolMainTools.Name = "toolMainTools";
            toolMainTools.DropDown = contextTools;

            Name = "FlowExplorer";

            tree.ListInfo.HeaderVisible = true;
            tree.ListInfo.HeaderWidth = 35;

            Localize();
        }

        private void OnDBSchemaChanged(object sender, DBSchemaChangedArgs e)
        {
            try
            {
                if (e.Type == DDLType.Create)
                {
                    //List<int> groups = FlowEnvir.GetGroups(FlowEnvir.Personal.User);

                    if (e.Item is DBTable && e.Item.Container != null)
                    {
                        var sgroup = GroupPermission.Get(null, e.Item.Schema);
                        var tgroup = GroupPermission.Get(sgroup, e.Item);

                        foreach (DBColumn column in ((DBTable)e.Item).Columns)
                        {
                            GroupPermission.Get(tgroup, column);
                        }
                    }
                    if (e.Item is DBColumn && e.Item.Container != null && ((DBColumn)e.Item).Table.Container != null)
                    {
                        var sgroup = GroupPermission.Get(null, e.Item.Schema);
                        var tgroup = GroupPermission.Get(sgroup, ((DBColumn)e.Item).Table);
                        GroupPermission.Get(tgroup, e.Item);
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
            }
        }

        private ILayoutCellEditor OptionsGetCellEditor(object sender, object listItem, ILayoutCell cell)
        {
            if (cell.CellEditor == null)
                return PDocument.InitCellEditor(sender, listItem, cell);
            else
                return null;
        }

        private void AcceptOnActivated(object sender, EventArgs e)
        {
            if (se.DataSource is DBItem)
            {
                ((DBItem)se.DataSource).Save();

            }
        }

        private void ToolFilterTextChanged(object sender, EventArgs e)
        {
            tree.Nodes.DefaultView.FilterQuery.Parameters.Clear();

            if (toolMainFilter.Text.Length != 0)
                tree.Nodes.DefaultView.FilterQuery.Parameters.Add(typeof(Node), LogicType.And, "FullPath", CompareType.Like, toolMainFilter.Text);
            else
                tree.Nodes.DefaultView.FilterQuery.Parameters.Add(typeof(Node), LogicType.And, "IsExpanded", CompareType.Equal, true);
            tree.Nodes.DefaultView.UpdateFilter();

        }

        private void ToolGenerateDBClick(object sender, EventArgs e)
        {
            if (tree.SelectedNode != null)
            {
                var query = new DataQuery();
                query.Query = GenereteExport(tree);
                query.ShowDialog(this);
            }
        }

        public static string GenereteExport(LayoutList tree)
        {
            StringBuilder rez = new StringBuilder();

            if (tree.SelectedNode != null)
            {
                foreach (var s in tree.Selection)
                {
                    Node node = (Node)s.Item;
                    if (node.Tag is DBItem)
                    {
                        rez.Append(DMLPatch((DBItem)node.Tag));
                    }
                    else if (node.Tag is IList)
                    {
                        foreach (DBItem item in (IEnumerable)node.Tag)
                        {
                            rez.Append(DMLPatch(item));
                        }
                    }
                }
            }
            return rez.ToString();
        }

        public static string DMLPatch(DBItem item)
        {
            StringBuilder rez = new StringBuilder();
            rez.AppendLine(string.Format("if exists(select * from {0} where {1}={2})", item.Table.Name, item.Table.PrimaryKey.Name, item.PrimaryId));
            rez.AppendLine("    " + item.Table.System.FormatCommand(item.Table, DBCommandTypes.Update, item) + ";");
            rez.AppendLine("else");
            rez.AppendLine("    " + item.Table.System.FormatCommand(item.Table, DBCommandTypes.Insert, item) + ";");
            rez.AppendLine();
            return rez.ToString();
        }

        public class StatWindow : ToolWindow
        {
            LayoutList users = new LayoutList();
            LayoutList stats = new LayoutList();
            GroupBox map = new GroupBox();

            public StatWindow()
            {
                Mode = ToolShowMode.Dialog;
                ButtonClose.Visible = false;

                users.ListSource = UDPService.Default.List;
                stats.ListSource = NetStat.Items;

                map.Add(new GroupBoxItem() { Widget = users, Text = "Users" });
                map.Add(new GroupBoxItem() { Widget = stats, Text = "Statistic", Row = 1 });
                Target = map;
            }

        }

        private void ToolStatClick(object sender, EventArgs e)
        {
            if (UDPService.Default != null)
            {
                var window = new StatWindow();
                window.Show(this, new Point());
            }
        }

        private void ContextAddItemClicked(object sender, EventArgs e)
        {
            var item = sender as GlyphMenuItem;

            DBItem row = null;
            object tag = tree.SelectedNode == null ? null : tree.SelectedNode.Tag;
            if (item == toolAddWork)
            {
                row = new Work();
            }
            else if (item == toolAddStage)
            {
                row = new Stage();
                if (tag is Work)
                    ((Stage)row).Work = (Work)tag;
            }
            else if (item == toolAddStageParam)
            {
                row = new StageParam();
                if (tag is Stage)
                    ((StageParam)row).Stage = (Stage)tag;
            }
            else if (item == toolAddGroup)
            {
                row = new UserGroup();
            }
            else if (item == toolAddTemplate)
            {
                row = new Template();
                if (tag is Template)
                    ((Template)row).Parent = (Template)tag;
            }
            else if (item == toolAddTemplateParam)
            {
                row = new TemplateParam();
                if (tag is Template)
                    ((TemplateParam)row).Template = (Template)tag;
            }
            else if (item == toolAddUser)
            {
                row = new User();
                if (tag is User && ((User)tag).IsCompaund)
                    ((User)row).Parent = (User)tag;
                //row.Access.Create
                for (int i = 0; i < row.Access.Items.Count; i++)
                {
                    var access = row.Access.Items[i];
                    access.Create = false;
                    row.Access.Add(access);
                }
            }
            else if (item == toolAddScheduler)
            {
                row = new Scheduler();
                if (tag is DBProcedure)
                    ((Scheduler)row).Procedure = (DBProcedure)tag;
            }
            ShowItem(row);
        }

        private void ShowItem(DBItem item)
        {
            if (item is UserGroup)
            {
                var group = (UserGroup)item;
                var editor = GuiService.Main.DockPanel.Find(PermissionEditor.GetName(group)) as PermissionEditor;
                if (editor == null)
                    editor = new PermissionEditor() { Group = group };
                GuiService.Main.DockPanel.Put(editor, DockType.Content);
            }
            else if (item is User)
            {
                if (item == User.CurrentUser && !((User)item).Super.Value)
                {
                    MessageDialog.ShowMessage(ParentWindow, "Unable edit current user!", "Access");
                    return;
                }
                UserEditor editor = new UserEditor();
                editor.User = (User)item;
                editor.ShowWindow(this);
            }
            else if (item is DBItem)
            {
                se.DataSource = item;
                se.List.EditState = item.Attached ? EditListState.Edit : EditListState.EditAny;
                ose.Mode = ToolShowMode.Dialog;
                ose.Show(this, new Point(0, 0));
            }
        }

        private void ToolMainRefreshClick(object sender, EventArgs e)
        {
            FlowEnvironment.LoadBooks();
        }

        private void ToolMainCopyClick(object sender, EventArgs e)
        {
            if (tree.SelectedNode == null)
                return;
            foreach (var s in tree.Selection)
            {
                Node tn = (Node)s.Item;
                object obj = tn.Tag;

                if (obj is DBItem)
                {
                    var row = (DBItem)((DBItem)obj).Clone();
                    if (row is User)
                    {
                        ((User)row).Super = false;
                    }
                    ShowItem(row);
                }
            }
        }

        private void ToolMainRemoveClick(object sender, EventArgs e)
        {
            if (tree.SelectedNode == null)
                return;
            var question = new QuestionMessage("Deleting", "Delete selected items?");
            question.Buttons.Add(Command.No);
            question.Buttons.Add(Command.Yes);
            if (MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes)
            {
                var items = tree.Selection.GetItems<Node>();
                foreach (Node tn in items)
                {
                    object obj = tn.Tag;

                    if (obj is DBItem)
                    {
                        ((DBItem)obj).Delete();
                        ((DBItem)obj).Save();
                    }
                }
            }
        }

        private void TreeAfterSelect(object sender, EventArgs e)
        {
            ShowNodeProperty();
        }

        private void TreeNodeMouseClick(object sender, EventArgs e)
        {
            ShowNodeProperty();
        }

        private void TreeNodeMouseDoubleClick(object sender, EventArgs e)
        {
            if (tree.SelectedNode != null && tree.SelectedNode.Tag != null)
            {
                if (tree.SelectedNode.Tag is IList)
                {
                    //FieldsEditor op = new FieldsEditor();
                    //op.Text = tree.SelectedNode.Header + (tree.SelectedNode.Group != null ? "(" + tree.SelectedNode.Group.ToString() + ")" : string.Empty);
                    //op.DataSource = (IList)tree.SelectedNode.Tag;
                    //op.GetCellEditor += OptionsGetCellEditor;
                    //op.DockType = tool.DockType.Content;
                    //GuiService.Main.DockPanel.Put(op, tool.DockType.Content);
                }
                else
                {
                    var access = tree.SelectedNode.Tag as IAccessable;
                    if (access.Access.Edit)
                    {
                        ShowItem((DBItem)tree.SelectedNode.Tag);
                    }
                    else
                    {
                        MessageDialog.ShowMessage(ParentWindow, Locale.Get(base.Name, "Access denied!"), Locale.Get(base.Name, "Access."));
                    }
                }
            }
        }

        public void ShowNodeProperty()
        {
            var flag = true;
            foreach (var select in tree.Selection)
            {
                var item = ((Node)select.Item).Tag as DBItem;
                if (item == null || !item.Access.Delete)
                {
                    flag = false;
                    break;
                }
            }
            this.toolMainRemove.Sensitive = flag;

            if (tree.SelectedNode != null && GuiService.Main != null && !(tree.SelectedNode.Tag is IDBTableView))
                GuiService.Main.ShowProperty(this, tree.SelectedNode.Tag, false);
        }

        public DockType DockType
        {
            get { return DockType.Left; }
        }

        public bool HideOnClose
        {
            get { return true; }
        }

        #region ILocalizable implementation

        public void Localize()
        {
            GuiService.Localize(toolRefresh, Name, "Refresh", GlyphType.Refresh);
            GuiService.Localize(toolMainCopy, Name, "Copy", GlyphType.CopyAlias);
            GuiService.Localize(toolMainAdd, Name, "Add", GlyphType.PlusCircle);
            GuiService.Localize(toolMainRemove, Name, "Remove", GlyphType.MinusCircle);
            GuiService.Localize(toolMainTools, Name, "Tools", GlyphType.Wrench);
            GuiService.Localize(toolGenerateDB, Name, "Generate Flow DB", GlyphType.Database);
            GuiService.Localize(toolConfig, Name, "Configuration", GlyphType.Tags);
            GuiService.Localize(toolStat, Name, "Statistic", GlyphType.Link);

            GuiService.Localize(toolAddTemplate, typeof(Template).FullName, "Template", GlyphType.Book);
            GuiService.Localize(toolAddTemplateParam, typeof(TemplateParam).FullName, "TemplateParam", GlyphType.Columns);
            GuiService.Localize(toolAddWork, typeof(Work).FullName, "Work", GlyphType.Building);
            GuiService.Localize(toolAddStage, typeof(Stage).FullName, "Stage", GlyphType.Flickr);
            GuiService.Localize(toolAddStageParam, typeof(StageParam).FullName, "StageParam", GlyphType.Columns);
            GuiService.Localize(toolAddUser, typeof(User).FullName, "User", GlyphType.User);
            GuiService.Localize(toolAddGroup, typeof(UserGroup).FullName, "Group", GlyphType.UserMd);
            GuiService.Localize(toolAddScheduler, typeof(Scheduler).FullName, "Scheduler", GlyphType.ClockO);

            GuiService.Localize(this, Name, "Flow explorer", GlyphType.Wrench);

            tree.Localize();
        }

        #endregion
    }
}

