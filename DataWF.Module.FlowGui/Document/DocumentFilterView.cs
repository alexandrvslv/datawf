using DataWF.Gui;
using DataWF.Module.Flow;

using Xwt;
using DataWF.Module.CommonGui;
using DataWF.Common;
using System;
using DataWF.Module.Common;

namespace DataWF.Module.FlowGui
{
    public class DocumentFilterView : VPanel
    {
        static readonly Invoker<TableItemNode, int> countInvoker = new Invoker<TableItemNode, int>(nameof(TableItemNode.Count), p => p.Count);

        private ToolSearchEntry toolFilter;
        private ToolItem toolClear;
        private Toolsbar bar;

        private LayoutList fields;

        private FlowTree templates;
        private UserTree users;
        private FlowTree works;

        //private VBox dates;
        //private FieldEditor dateType;
        //private CalendarEditor date;

        GroupBox map;
        private DocumentFilter filter;

        public DocumentFilterView()
        {
            toolFilter = new ToolSearchEntry() { Name = "Filter", FillWidth = true };
            toolClear = new ToolItem(ToolClearClick) { Name = "Clear", Glyph = GlyphType.Eraser };
            bar = new Toolsbar(toolClear, toolFilter);

            fields = new LayoutList()
            {
                GenerateFields = false,
                FieldInfo = new LayoutFieldInfo(
                    new LayoutField { Name = nameof(DocumentFilter.Number) },
                    new LayoutField { Name = nameof(DocumentFilter.IsWork) },
                    new LayoutField { Name = nameof(DocumentFilter.IsCurrent) },
                    new LayoutField { Name = nameof(DocumentFilter.Customer) },
                    new LayoutField { Name = nameof(DocumentFilter.DateType) },
                    new LayoutField { Name = nameof(DocumentFilter.Date) }
                    ),
                Name = "Basic"
            };

            templates = new FlowTree
            {
                FlowKeys = FlowTreeKeys.Template,
                Name = "Templates",
                FilterEntry = toolFilter.Entry
            };
            templates.ListInfo.Columns.Add(
                new LayoutColumn
                {
                    Name = nameof(TableItemNode.Count),
                    Width = 35,
                    Style = GuiEnvironment.StylesInfo["CellFar"],
                    Invoker = countInvoker
                });
            works = new FlowTree
            {
                FlowKeys = FlowTreeKeys.Work | FlowTreeKeys.Stage,
                Name = "Works_Stage"
            };
            //dates = new VBox();
            //dates.PackStart(dateType)

            users = new UserTree()
            {
                UserKeys = UserTreeKeys.Department | UserTreeKeys.Position | UserTreeKeys.User,
                Name = "Users"
            };


            map = new GroupBox(
                        new GroupBoxItem { Widget = fields, Name = "Filter", FillWidth = true },
                        new GroupBoxItem { Widget = templates, Row = 1, Name = "Templates", FillWidth = true, FillHeight = true },
                        new GroupBoxItem { Widget = works, Row = 2, Name = "Works & Stage", FillWidth = true, Expand = false },
                        new GroupBoxItem { Widget = users, Row = 3, Name = "Users", FillWidth = true, Expand = false }
                        )
            { Name = "Map" };

            PackStart(bar, false, false);
            PackStart(map, true, true);

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
        }

        public void Localize()
        {
            bar.Localize();
            map.Localize();
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

        private void ToolClearClick(object sender, EventArgs e)
        {
            Filter?.Clear();
        }

        public DocumentFilter Filter
        {
            get => filter;
            set
            {
                if (filter != value)
                {
                    filter = value;
                    fields.FieldSource = value;

                    templates.Bind(filter, nameof(DocumentFilter.Template));
                    works.Bind(filter, nameof(DocumentFilter.Stage));
                    users.Bind(filter, nameof(DocumentFilter.Staff));
                }
            }
        }

        public FlowTree Templates { get => templates; }

        public FlowTree Works { get => works; }

        public UserTree Users { get => users; }

        protected override void Dispose(bool disposing)
        {
            //if (filter != null)
            //    filter.Dispose();

            base.Dispose(disposing);
        }
    }
}
