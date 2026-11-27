using DataWF;

namespace DataWF.Gui
{
    public class ToolsbarEditor : VPanel
    {
        private ListEditor toolList;
        private LayoutList details;
        private Toolsbar bar;

        public ToolsbarEditor()
        {
            details = new LayoutList();

            toolList = new ListEditor(new LayoutList()
            {
                GenerateColumns = false,
                GenerateToString = false,
                ListInfo = new LayoutListInfo(
                    new LayoutColumn { Name = nameof(ToolItem.ToString), Editable = false },
                    new LayoutColumn { Name = nameof(ToolItem.Text), FillWidth = true },
                    new LayoutColumn { Name = nameof(ToolItem.IsVisible), Width = 50 },
                    new LayoutColumn { Name = nameof(ToolItem.Glyph) })
                { }
            });
            toolList.List.Bind(details, nameof(LayoutList.FieldSource), nameof(LayoutList.SelectedItem));

            var map = new GroupBox(
                new GroupBoxItem { Name = "Items", Control = toolList, FillWidth = true, FillHeight = true },
                new GroupBoxItem { Name = "Details", Control = details, FillWidth = true, FillHeight = true, Row = 1 });
            Children.Add(map, true, true);
        }

        public Toolsbar Bar
        {
            get => bar;
            set
            {
                if (bar != value)
                {
                    bar = value;
                    toolList.DataSource = new SelectableList<ToolItem>(bar.Items.GetItems());
                }
            }
        }
    }
}
