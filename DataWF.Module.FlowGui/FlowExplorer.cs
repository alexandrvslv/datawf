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
using DataWF.Module.CommonGui;

namespace DataWF.Module.FlowGui
{
    [Module(true)]
    public class FlowExplorer : VPanel, IDockContent
    {
        private ToolWindow ose;
        private ListEditor se;
        private Toolsbar barMain;
        private Menubar contextAdd;
        private Menubar contextTools;
        private FlowTree tree;

        public FlowExplorer()
            : base()
        {
            contextTools = new Menubar(
                new ToolMenuItem(ToolMainRefreshClick) { Name = "Refresh", Glyph = GlyphType.Refresh },
                new ToolMenuItem(ToolGenerateDBClick) { Name = "Generate database", Glyph = GlyphType.Database },
                new ToolMenuItem(ToolStatClick) { Name = "Stats", Glyph = GlyphType.Link })
            { Name = "FlowExplorer" };

            contextAdd = new Menubar(
                new ToolMenuItem { Name = "Template", Sensitive = Template.DBTable?.Access.Create ?? false, Glyph = GlyphType.Book },
                new ToolMenuItem { Name = "Template Attribute", Sensitive = TemplateParam.DBTable?.Access.Create ?? false, Glyph = GlyphType.Columns },
                new ToolMenuItem { Name = "Work", Sensitive = Work.DBTable?.Access.Create ?? false, Glyph = GlyphType.GearsAlias },
                new ToolMenuItem { Name = "Work Stage", Sensitive = Stage.DBTable?.Access.Create ?? false, Glyph = GlyphType.EditAlias },
                new ToolMenuItem { Name = "Stage Parameter", Sensitive = StageParam.DBTable?.Access.Create ?? false, Glyph = GlyphType.Columns },
                new ToolMenuItem { Name = "Group", Sensitive = UserGroup.DBTable?.Access.Create ?? false, Glyph = GlyphType.Users },
                new ToolMenuItem { Name = "Department", Sensitive = Department.DBTable?.Access.Create ?? false, Glyph = GlyphType.Home },
                new ToolMenuItem { Name = "Position", Sensitive = Position.DBTable?.Access.Create ?? false, Glyph = GlyphType.UserMd },
                new ToolMenuItem { Name = "User", Sensitive = User.DBTable?.Access.Create ?? false, Glyph = GlyphType.User },
                new ToolMenuItem { Name = "Scheduler", Sensitive = Scheduler.DBTable?.Access.Create ?? false, Glyph = GlyphType.ClockO })
            { Name = "FlowExplorer" };
            contextAdd.Bar.ItemClick += ContextAddItemClicked;

            barMain = new Toolsbar(
                new ToolDropDown { Name = "Add", ForeColor = Colors.DarkGreen, DropDown = contextAdd, Glyph = GlyphType.PlusCircle },
                new ToolItem(ToolMainRemoveClick) { Name = "Remove", ForeColor = Colors.DarkRed, Glyph = GlyphType.MinusCircle },
                new ToolItem(ToolMainCopyClick) { Name = "Copy", Glyph = GlyphType.CopyAlias },
                new ToolDropDown { Name = "Tools", DropDown = contextTools, Glyph = GlyphType.Wrench },
                new ToolSearchEntry() { Name = "FilterText", FillWidth = true })
            { Name = "FlowExplorer" };

            se = new ListEditor();

            ose = new ToolWindow { Target = se };
            ose.ButtonAcceptClick += AcceptOnActivated;

            var userKeys = UserTreeKeys.None;
            if (Department.DBTable?.Access.View ?? false) userKeys |= UserTreeKeys.Department;
            if (Position.DBTable?.Access.View ?? false) userKeys |= UserTreeKeys.Position;
            if (User.DBTable?.Access.View ?? false) userKeys |= UserTreeKeys.User;
            if (UserGroup.DBTable?.Access.View ?? false) userKeys |= UserTreeKeys.Group;
            if (Scheduler.DBTable?.Access.View ?? false) userKeys |= UserTreeKeys.Scheduler;
            var keys = FlowTreeKeys.None;
            if (TemplateParam.DBTable?.Access.View ?? false) keys |= FlowTreeKeys.TemplateParam;
            if (Template.DBTable?.Access.View ?? false) keys |= FlowTreeKeys.Template;
            if (StageParam.DBTable?.Access.View ?? false) keys |= FlowTreeKeys.StageParam;
            if (Stage.DBTable?.Access.View ?? false) keys |= FlowTreeKeys.Stage;
            if (Work.DBTable?.Access.View ?? false) keys |= FlowTreeKeys.Work;
            tree = new FlowTree
            {
                Status = DBStatus.Empty,
                FlowKeys = keys,
                UserKeys = userKeys,
                FilterEntry = ((ToolSearchEntry)barMain["FilterText"]).Entry
            };
            tree.ListInfo.HeaderVisible = true;
            tree.ListInfo.HeaderWidth = 35;
            tree.SelectionChanged += TreeAfterSelect;
            tree.CellMouseClick += TreeNodeMouseClick;
            tree.CellDoubleClick += TreeNodeMouseDoubleClick;

            Name = "FlowExplorer";
            PackStart(barMain, false, false);
            PackStart(tree, true, true);
        }

        private void AcceptOnActivated(object sender, EventArgs e)
        {
            if (se.DataSource is DBItem)
            {
                ((DBItem)se.DataSource).Save();

            }
        }

        private void ToolGenerateDBClick(object sender, EventArgs e)
        {
            if (tree.SelectedNode != null)
            {
                var query = new DataQuery();
                query.Query = tree.GenereteExport();
                query.ShowDialog(this);
            }
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

        private void ContextAddItemClicked(object sender, ToolItemEventArgs e)
        {
            var item = e.Item;

            DBItem row = null;
            object tag = tree.SelectedDBItem;
            if (item.Name == "Work")
            {
                row = new Work();
            }
            else if (item.Name == "Work Stage")
            {
                row = new Stage();
                if (tag is Work)
                    ((Stage)row).Work = (Work)tag;
            }
            else if (item.Name == "Stage Parameter")
            {
                row = new StageParam();
                if (tag is Stage)
                    ((StageParam)row).Stage = (Stage)tag;
            }
            else if (item.Name == "Group")
            {
                row = new UserGroup();
            }
            else if (item.Name == "Template")
            {
                row = new Template();
                if (tag is Template)
                    ((Template)row).Parent = (Template)tag;
            }
            else if (item.Name == "Template Attribute")
            {
                row = new TemplateParam();
                if (tag is Template)
                    ((TemplateParam)row).Template = (Template)tag;
            }
            else if (item.Name == "User")
            {
                row = new User();
                if (tag is Department)
                    ((User)row).Department = (Department)tag;
                //row.Access.Create
                for (int i = 0; i < row.Access.Items.Count; i++)
                {
                    var access = row.Access.Items[i];
                    access.Create = false;
                    row.Access.Add(access);
                }
            }
            else if (item.Name == "Scheduler")
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
                ose.Title = $"{item.Table} ({item})";
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
            if (tree.SelectedDBItem != null)
            {
                var access = tree.SelectedDBItem as IAccessable;
                if (access.Access.Edit)
                {
                    ShowItem(tree.SelectedDBItem);
                }
                else
                {
                    MessageDialog.ShowMessage(ParentWindow, Locale.Get(base.Name, "Access denied!"), Locale.Get(base.Name, "Access."));
                }
            }
        }

        public void ShowNodeProperty()
        {
            var flag = true;
            foreach (var select in tree.Selection)
            {
                var item = ((TableItemNode)select.Item).Item as DBItem;
                if (item == null || !item.Access.Delete)
                {
                    flag = false;
                    break;
                }
            }
            barMain["Remove"].Sensitive = flag;

            if (tree.SelectedDBItem != null && GuiService.Main != null)
                GuiService.Main.ShowProperty(this, tree.SelectedDBItem, false);
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
            barMain.Localize();
            tree.Localize();
            GuiService.Localize(this, Name, "Flow Config", GlyphType.Wrench);
        }

        #endregion
    }
}

