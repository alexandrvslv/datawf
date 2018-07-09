using DataWF.Gui;
using DataWF.Module.Flow;
using DataWF.Module.CommonGui;
using DataWF.Common;
using System;
using DataWF.Module.Common;
using DataWF.Data.Gui;

namespace DataWF.Module.FlowGui
{
    public class DocumentFilterView : VPanel
    {
        static readonly Invoker<TableItemNode, int> countInvoker = new Invoker<TableItemNode, int>(nameof(TableItemNode.Count), p => p.Count);

        private ToolSearchEntry toolFilter;

        private LayoutList fields;

        private FlowTree templates;
        private UserTree users;
        private FlowTree works;

        //private VBox dates;
        //private FieldEditor dateType;
        //private CalendarEditor date;

        GroupBox box;
        private DocumentFilter filter;

        public DocumentFilterView()
        {
            toolFilter = new ToolSearchEntry() { Name = "Filter", FillWidth = true };

            fields = new LayoutList()
            {
                EditMode = EditModes.ByClick,
                GenerateFields = false,
                GenerateColumns = false,
                FieldInfo = new LayoutFieldInfo(
                    new LayoutField { Name = nameof(DocumentFilter.IsWork) },
                    new LayoutField { Name = nameof(DocumentFilter.IsCurrent) },
                    new LayoutField { Name = nameof(DocumentFilter.Number) },
                    new LayoutField { Name = nameof(DocumentFilter.Title) },
                    new LayoutField { Name = nameof(DocumentFilter.Customer) },
                    new LayoutField { Name = nameof(DocumentFilter.DateType) },
                    new LayoutField { Name = nameof(DocumentFilter.Date) }
                    ),
                Name = "Basic"
            };

            templates = new FlowTree
            {
                ShowListNode = false,
                FlowKeys = FlowTreeKeys.Template,
                Name = "Templates",
                FilterEntry = toolFilter.Entry
            };
            templates.ListInfo.Columns.Add(
                new LayoutColumn
                {
                    Name = nameof(TableItemNode.Count),
                    Width = 35,
                    Style = GuiEnvironment.Theme["CellFar"],
                    Invoker = countInvoker
                });
            works = new FlowTree
            {
                ShowListNode = false,
                FlowKeys = FlowTreeKeys.Work | FlowTreeKeys.Stage,
                Name = "Works_Stage"
            };
            //dates = new VBox();
            //dates.PackStart(dateType)

            users = new UserTree()
            {
                ShowListNode = false,
                UserKeys = UserTreeKeys.Department | UserTreeKeys.Position | UserTreeKeys.User | UserTreeKeys.Current,
                Name = "Users"
            };

            box = new GroupBox(
                new GroupBoxItem { Widget = templates, Name = "Document Type", FillWidth = true, FillHeight = true },
                //new GroupBoxItem { Widget = fields, Row = 1, Name = "Parameters", FillWidth = true, Height = 160, Autosize = false, Expand = false },
                new GroupBoxItem { Widget = works, Row = 2, Name = "Workflow & Stage", FillWidth = true, FillHeight = true, Expand = false },
                new GroupBoxItem { Widget = users, Row = 3, Name = "Staff", FillWidth = true, FillHeight = true, Expand = false })
            { Name = "Map" };

            PackStart(box, true, true);
            MinWidth = 330;

            var nodeSend = new TableItemNode()
            {
                Name = "Send",
                Tag = new DocumentFilter()
                {
                    Staff = User.CurrentUser,
                    DateType = DocumentSearchDate.WorkEnd,
                    Date = new DateInterval(DateTime.Today),
                    IsWork = CheckedState.Unchecked
                }
            };
            GuiService.Localize(nodeSend, "DocumentWorker", nodeSend.Name);

            var nodeRecent = new TableItemNode()
            {
                Name = "Recent",
                Tag = new DocumentFilter()
                {
                    Staff = User.CurrentUser,
                    DateType = DocumentSearchDate.WorkEnd,
                    Date = new DateInterval(DateTime.Today)
                }
            };
            GuiService.Localize(nodeRecent, "DocumentWorker", nodeRecent.Name);

            var nodeSearch = new TableItemNode()
            {
                Name = "Search",
                Tag = new DocumentFilter() { }
            };
            GuiService.Localize(nodeSearch, "DocumentWorker", nodeSearch.Name);

            Name = nameof(DocumentFilterView);
        }
        public DocumentFilter Filter
        {
            get => filter;
            set
            {
                if (filter != value)
                {
                    filter = value;
                    //fields.FieldSource = value;
                    BindTemplates();
                    works.Bind(filter, nameof(DocumentFilter.Stage), nameof(UserTree.SelectedDBItem));
                    users.Bind(filter, nameof(DocumentFilter.Staff), nameof(UserTree.SelectedDBItem));
                }
            }
        }

        public void BindTemplates()
        {
            templates.Bind(filter, nameof(DocumentFilter.Template), nameof(UserTree.SelectedDBItem));
        }

        public void UnbindTemplates()
        {
            templates.Unbind();
        }

        public FlowTree Templates { get => templates; }

        public FlowTree Works { get => works; }

        public UserTree Users { get => users; }

        public GroupBox Box { get => box; }

        public override void Localize()
        {
            base.Localize();
            box.Localize();
        }

        private void WorksSelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            if (works.SelectedItem != null && filter != null)
            {
                filter.Stage = works.SelectedDBItem;
            }
        }

        private void UsersSelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            if (users.SelectedItem != null && filter != null)
            {
                filter.Staff = users.SelectedDBItem;
            }
        }

        private void TemplatesSelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            if (templates.SelectedItem != null && filter != null)
            {
                filter.Template = (Template)templates.SelectedDBItem;
            }
        }

        protected override void Dispose(bool disposing)
        {
            //if (filter != null)
            //    filter.Dispose();

            base.Dispose(disposing);
        }
    }
}
