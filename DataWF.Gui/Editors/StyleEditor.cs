using DataWF;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DataWF.Gui
{
    public class StyleCanvas : Canvas
    {
        private Array values;
        private CellStyle style;

        public StyleCanvas()
        {
            values = Enum.GetValues(typeof(CellDisplayState));
            MinWidth = 200;
            MinHeight = 170;
        }

        public CellStyle Style
        {
            get => style;
            set
            {
                style = value;
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext ctx)
        {
            base.Render(ctx);
            var dirtyRect = Bounds;
            ctx.FillRectangle(Brushes.White, dirtyRect, 10F);

            using (var context = new GraphContext(ctx))
            {
                if (style != null)
                {
                    int y = 10;
                    foreach (var value in values)
                    {
                        var text = new FormattedText { Text = value.ToString() };

                        ctx.DrawText(Brushes.Black, new Point(5, y), text);
                        context.DrawCell(Style, "Test Text",
                            new Rect(100, y, Width - 120, 20),
                            new Rect(100, y, Width - 120, 20),
                            (CellDisplayState)value);
                        y += 30;
                    }
                }
            }
        }
    }

    public class StyleEditor : VPanel, ILocalizable
    {
        private ListEditor list = new ListEditor();
        private LayoutList details = new LayoutList();
        private GroupBox map = new GroupBox();
        private StyleCanvas preview = new StyleCanvas();

        public StyleEditor()
        {
            list.Name = "listEditor1";
            list.DataSource = GuiEnvironment.Theme;
            list.ReadOnly = false;
            list.List.SelectionChanged += ListItemSelect;

            details.EditMode = EditModes.ByClick;

            var smap = new GroupBoxItem(map) { Row = 1 };
            smap.Add(new GroupBoxItem() { Control = details, Text = "Detail", FillWidth = true, Width = 230, Height = 60 });
            smap.Add(new GroupBoxItem() { Control = preview, Text = "Preview", FillWidth = true, Width = 230, Height = 60 });

            map.Add(new GroupBoxItem() { Control = list, Text = "Slyles", FillHeight = true, FillWidth = true });
            map.Add(smap);

            Name = "StyleEditor";
            Text = "Style Editor";
            Children.Add(map);

            Localize();
        }

        private void ListItemSelect(object sender, EventArgs e)
        {
            if (list.List.SelectedItem is CellStyle style)
            {
                details.FieldSource = style;
                preview.Style = style;
            }
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, GetType().Name, "Localize Editor");
        }
    }

}
