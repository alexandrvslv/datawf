using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Xwt.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Xwt;

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
            get { return style; }
            set
            {
                style = value;
                QueueDraw();
            }
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);
            ctx.SetColor(Colors.White);
            ctx.RoundRectangle(dirtyRect, 10);
            ctx.Fill();

            var context = GraphContext.Default;
            context.Context = ctx;

            if (style != null)
            {
                int y = 10;
                foreach (var value in values)
                {
                    using (TextLayout text = new TextLayout(this) { Text = value.ToString() })
                    {
                        ctx.SetColor(Colors.Black);
                        ctx.DrawTextLayout(text, new Point(5, y));
                    }
                    context.DrawCell(Style, "Test Text",
                        new Rectangle(100, y, Size.Width - 120, 20),
                        new Rectangle(100, y, Size.Width - 120, 20),
                        (CellDisplayState)value);
                    y += 30;
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
            smap.Add(new GroupBoxItem() { Widget = details, Text = "Detail", FillWidth = true, Width = 230, Height = 60 });
            smap.Add(new GroupBoxItem() { Widget = preview, Text = "Preview", FillWidth = true, Width = 230, Height = 60 });

            map.Add(new GroupBoxItem() { Widget = list, Text = "Slyles", FillHeight = true, FillWidth = true });
            map.Add(smap);

            this.Name = "StyleEditor";
            this.Text = "Style Editor";
            this.PackStart(map, true, true);

            Localize();
        }

        private void ListItemSelect(object sender, EventArgs e)
        {
            var style = list.List.SelectedItem as CellStyle;
            if (style != null)
            {
                details.FieldSource = style;
                preview.Style = style;
            }
        }

        public void Localize()
        {
            GuiService.Localize(this, GetType().Name, "Localize Editor");
            list.Localize();
            details.Localize();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }
    }

}
