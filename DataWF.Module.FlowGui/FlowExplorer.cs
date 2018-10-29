using DataWF.Common;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Module.Common;
using DataWF.Module.CommonGui;
using DataWF.Module.Flow;
using System;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Module.FlowGui
{
    [Module(true)]
    public partial class FlowExplorer : VPanel, IDockContent
    {
        private ToolWindow ose;
        private ListEditor se;
        private Toolsbar barMain;
        private Menubar contextAdd;
        private Menubar contextTools;
        private FlowTree tree;
        private DBItem currentItem;

        public FlowExplorer()
            : base()
        {
            contextTools = new Menubar(
                new ToolMenuItem(ToolMainRefreshClick) { Name = "Refresh", Glyph = GlyphType.Refresh },
                new ToolMenuItem(ToolGenerateDBClick) { Name = "Generate database", Glyph = GlyphType.Database },
                new ToolMenuItem(ToolStatClick) { Name = "Stats", Glyph = GlyphType.Link })
            { Name = "FlowExplorer" };

            contextAdd = new Menubar(
                new ToolMenuItem
                {
                    Name = "Template",
                    Sensitive = Template.DBTable?.Access.GetFlag(AccessType.Create, GuiEnvironment.CurrentUser) ?? false,
                    Glyph = GlyphType.Book
                },
                 new ToolMenuItem
                 {
                     Name = "Template Data",
                     Sensitive = TemplateData.DBTable?.Access.GetFlag(AccessType.Create, GuiEnvironment.CurrentUser) ?? false,
                     Glyph = GlyphType.File
                 },
                new ToolMenuItem
                {
                    Name = "Work",
                    Sensitive = Work.DBTable?.Access.GetFlag(AccessType.Create, GuiEnvironment.CurrentUser) ?? false,
                    Glyph = GlyphType.GearsAlias
                },
                new ToolMenuItem
                {
                    Name = "Work Stage",
                    Sensitive = Stage.DBTable?.Access.GetFlag(AccessType.Create, GuiEnvironment.CurrentUser) ?? false,
                    Glyph = GlyphType.EditAlias
                },
                new ToolMenuItem
                {
                    Name = "Stage Parameter",
                    Sensitive = StageParam.DBTable?.Access.GetFlag(AccessType.Create, GuiEnvironment.CurrentUser) ?? false,
                    Glyph = GlyphType.Columns,
                    DropDown = new Menubar(
                        new ToolMenuItem
                        {
                            Name = "Stage Procedure",
                            Sensitive = StageParam.DBTable?.Access.GetFlag(AccessType.Create, GuiEnvironment.CurrentUser) ?? false,
                            Glyph = GlyphType.EditAlias
                        },
                        new ToolMenuItem
                        {
                            Name = "Stage Reference",
                            Sensitive = StageParam.DBTable?.Access.GetFlag(AccessType.Create, GuiEnvironment.CurrentUser) ?? false,
                            Glyph = GlyphType.EditAlias
                        }
                        )
                },
                new ToolMenuItem
                {
                    Name = "Group",
                    Sensitive = UserGroup.DBTable?.Access.GetFlag(AccessType.Create, GuiEnvironment.CurrentUser) ?? false,
                    Glyph = GlyphType.Users
                },
                new ToolMenuItem
                {
                    Name = "Department",
                    Sensitive = Department.DBTable?.Access.GetFlag(AccessType.Create, GuiEnvironment.CurrentUser) ?? false,
                    Glyph = GlyphType.Home
                },
                new ToolMenuItem { Name = "Position", Sensitive = Position.DBTable?.Access.Create ?? false, Glyph = GlyphType.UserMd },
                new ToolMenuItem { Name = "User", Sensitive = User.DBTable?.Access.Create ?? false, Glyph = GlyphType.User },
                new ToolMenuItem { Name = "Scheduler", Sensitive = Scheduler.DBTable?.Access.Create ?? false, Glyph = GlyphType.ClockO })
            { Name = "FlowExplorer" };
            contextAdd.Bar.ItemClick += ContextAddItemClicked;

            barMain = new Toolsbar(
                new ToolDropDown { Name = "Add", GlyphColor = Colors.DarkGreen, DropDown = contextAdd, Glyph = GlyphType.PlusCircle },
                new ToolItem(ToolMainRemoveClick) { Name = "Remove", GlyphColor = Colors.DarkRed, Glyph = GlyphType.MinusCircle },
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
            if (GroupPermission.DBTable?.Access.View ?? false) userKeys |= UserTreeKeys.Permission;
            if (User.DBTable?.Access.View ?? false) userKeys |= UserTreeKeys.User;
            if (UserGroup.DBTable?.Access.View ?? false) userKeys |= UserTreeKeys.Group;
            if (Scheduler.DBTable?.Access.View ?? false) userKeys |= UserTreeKeys.Scheduler;
            var keys = FlowTreeKeys.None;
            //if (TemplateParam.DBTable?.Access.View ?? false) keys |= FlowTreeKeys.TemplateParam;
            if (Template.DBTable?.Access.View ?? false) keys |= FlowTreeKeys.Template;
            if (StageParam.DBTable?.Access.View ?? false) keys |= FlowTreeKeys.StageParam;
            if (Stage.DBTable?.Access.View ?? false) keys |= FlowTreeKeys.Stage;
            if (Work.DBTable?.Access.View ?? false) keys |= FlowTreeKeys.Work;
            tree = new FlowTree
            {
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
                var query = new DataQuery { Query = tree.GenereteExport() };
                query.ShowDialog(this);
            }
        }

        private void ToolStatClick(object sender, EventArgs e)
        {
            if (NotifyService.Default != null)
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
            else if (item.Name == "Stage Procedure")
            {
                row = new StageProcedure();
                if (tag is Stage)
                    ((StageParam)row).Stage = (Stage)tag;
            }
            else if (item.Name == "Stage Reference")
            {
                row = new StageReference();
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
            else if (item.Name == "Template Data")
            {
                row = new TemplateData();
                if (tag is Template)
                    ((TemplateData)row).Template = (Template)tag;
            }
            else if (item.Name == "User")
            {
                row = new User();
                if (tag is Department)
                    ((User)row).Department = (Department)tag;
                else if (tag is Position)
                    ((User)row).Position = (Position)tag;
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
            if (item is UserGroup group)
            {
                if (!(GuiService.Main.DockPanel.Find(PermissionEditor.GetName(group)) is PermissionEditor editor))
                    editor = new PermissionEditor() { Group = group };
                GuiService.Main.DockPanel.Put(editor, DockType.Content);
            }
            else if (item is User user)
            {
                if (item == User.CurrentUser && !user.Super.Value)
                {
                    MessageDialog.ShowMessage(ParentWindow, "Unable edit current user!", "Access");
                    return;
                }
                var editor = new UserEditor { User = user };
                editor.ShowWindow(this);
            }
            else if (item is Template template)
            {
                var editor = new TemplateEditor { Template = template };
                editor.ShowWindow(this);
            }
            else if (item is Work work)
            {
                var editor = new WorkEditor { Work = work };
                editor.ShowWindow(this);
            }
            else if (item is Stage stage)
            {
                var editor = new StageEditor { Stage = stage };
                editor.ShowWindow(this);
            }
            else if (item != null)
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
            foreach (var node in tree.Selection.GetItems<TableItemNode>())
            {
                if (node != null && node.Item is DBItem dbItem)
                {
                    var row = (DBItem)dbItem.Clone();
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
                var items = tree.Selection.GetItems<TableItemNode>();
                foreach (var node in items)
                {

                    if (node.Item is DBItem dbItem)
                    {
                        dbItem.Delete();
                        dbItem.Save();
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
            foreach (var select in tree.Selection.GetItems<TableItemNode>())
            {
                if (!(select.Item is DBItem item) || !item.Access.Delete)
                {
                    flag = false;
                    break;
                }
            }
            barMain["Remove"].Sensitive = flag;
            CurrentItem = tree.SelectedDBItem;
        }

        public DBItem CurrentItem
        {
            get { return currentItem; }
            set
            {
                if (currentItem == value)
                    return;
                if (currentItem != null)
                {
                    currentItem.PropertyChanged -= CurrentItemPropertyChanged;
                }
                currentItem = value;
                if (currentItem != null)
                {
                    if (GuiService.Main != null)
                    {
                        GuiService.Main.ShowProperty(this, tree.SelectedDBItem, false);
                        currentItem.PropertyChanged += CurrentItemPropertyChanged;
                    }
                }
            }
        }

        private void CurrentItemPropertyChanged(object sender, EventArgs e)
        {
            var item = (DBItem)sender;
            if (item.IsChangedKey(item.Table.AccessKey))
            {
                var node = tree.Find(item);


                foreach (var entry in node.GetNodes())
                {
                    if (entry is TableItemNode && ((TableItemNode)entry).Item is DBItem)
                        ((DBItem)((TableItemNode)entry).Item).Access = item.Access;
                }
            }
        }

        public DockType DockType
        {
            get { return DockType.Left; }
        }

        public bool HideOnClose
        {
            get { return true; }
        }

        public bool Closing()
        {
            return true;
        }

        public void Activating()
        {
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, Name, "Flow Config", GlyphType.Wrench);
        }
    }
}

