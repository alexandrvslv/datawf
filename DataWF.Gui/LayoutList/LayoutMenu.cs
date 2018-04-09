using System;
using System.Collections.Generic;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class LayoutMenu : ToolWindow
    {
        //Menu MenuColumns = new Menu();
        //public Menu MenuFieldMenu = new Menu();
        //public Menu MenuFieldHideMenu = new Menu();
        //public GlyphMenuItem FieldColumns = new GlyphMenuItem();
        //public GlyphMenuItem FieldHide = new GlyphMenuItem();
        //public GlyphMenuItem FieldHided = new GlyphMenuItem();
        //public GlyphMenuItem FieldClear = new GlyphMenuItem();
        //public GlyphMenuItem FieldReset = new GlyphMenuItem();
        //public LogicType Logic = LogicType.Undefined;

        public LayoutMenu()
        {
            Target = new LayoutMenuEditor();
            Size = new Size(800, 600);
        }

        public LayoutMenuEditor Editor
        {
            get { return (LayoutMenuEditor)Target; }
        }

        protected override void OnAcceptClick(object sender, EventArgs e)
        {
            base.OnAcceptClick(sender, e);
            if (ContextList.TreeMode != ContextList.ListInfo.Tree)
            {
                ContextList.TreeMode = ContextList.ListInfo.Tree;
            }
            ContextList.RefreshBounds(true);
        }

        public LayoutList ContextList
        {
            get => Editor.ContextList;
            set
            {
                Editor.ContextList = value;
            }
        }

        public LayoutField ContextField { get => Editor.ContextField; set => Editor.ContextField = value; }

        public LayoutColumn ContextColumn { get => Editor.ContextColumn; set => Editor.ContextColumn = value; }

        private static Dictionary<LayoutField, ToolMenuItem> CacheMenuField = new Dictionary<LayoutField, ToolMenuItem>();
        internal static ToolMenuItem GetCached(LayoutField field)
        {
            return CacheMenuField.TryGetValue(field, out var menuItem) ? menuItem : null;
        }

        private static Dictionary<LayoutColumn, ToolMenuItem> CacheMenuColumn = new Dictionary<LayoutColumn, ToolMenuItem>();
        internal static ToolMenuItem GetCached(LayoutColumn column)
        {
            return CacheMenuColumn.TryGetValue(column, out var menuItem) ? menuItem : null;
        }

        public static ToolMenuItem BuildMenuItem(LayoutColumn column)
        {
            var item = new ToolMenuItem()
            {
                Text = column.Text,
                Name = column.Name,
                Checked = column.Map != null && column.Visible,
                Tag = column
            };
            //item.Click += defMenu.MenuSubColumnsItemClicked;
            return item;
        }

        public static ToolMenuItem BuilMenuItem(LayoutField f)
        {
            var item = LayoutMenu.GetCached(f);
            if (item == null)
            {
                item = new ToolMenuItem();
                item.Name = f.Name;
                item.Text = GroupHelper.GetFullName(f, " ");
                item.Tag = f;
                item.Click += (object sender, EventArgs e) =>
                {
                    var menuitem = (ToolMenuItem)sender;
                    ((LayoutField)menuitem.Tag).Visible = true;
                };
            }
            return item;
        }


#if GTK
        private static void OnMenuPrintClick(object sender, EventArgs e)
        {
            var print = new PrintOperation();
            print.BeginPrint += PrintBeginPrint;
            print.DrawPage += PrintDrawPage;
            print.Run(PrintOperationAction.Preview, (Gtk.Window)contextList.Toplevel);
        }

        private static void PrintDrawPage(object o, DrawPageArgs args)
        {
            GraphContext gc = new GraphContext(null);//TODO Gtk.DotNet.Graphics.FromDrawable(args.Context.CairoContext));
            gc.Print = true;
            gc.Scale = contextList.ListInfo.Scale;
            gc.Area = new System.Drawing.RectangleF(0, 0, (float)args.Context.Width, (float)args.Context.Height);
            var temp = new Point(0, (int)args.Context.Height * args.PageNr);
            contextList.OnPaintList(gc);
            gc.Dispose();
        }

        private static void PrintBeginPrint(object o, BeginPrintArgs args)
        {
            System.Drawing.RectangleF contextr = contextList.GetContentBound(contextList.GetColumnsBound());
            int rez = (int)(contextr.Height / args.Context.Height);
            print.NPages = rez;
        }


        private static void OnMenuPrintClick(object sender, EventArgs e)
        {
            //PageSetupDialog pd = new PageSetupDialog();
            var margins = new Margins(40, 10, 10, 20);
            var print = new Print();
            print.DefaultPageSettings.Margins = margins;
            print.DefaultPageSettings.Landscape = true;
            print.BeginPrint += PrintBeginPrint;
            print.PrintPage += PrintOnPrintPage;

            var dialog = new PrintDialog();
            dialog.UseEXDialog = true;
            dialog.Document = print;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                printpage = 0;
                print.Print();
            }
            //print = new PrintOperation ();
            //print.BeginPrint += PrintBeginPrint;
            //print.DrawPage += PrintDrawPage;
            //print.Run (PrintOperationAction.Preview, (Gtk.Window)this.Toplevel);
        }

        private static void PrintOnPrintPage(object sender, PrintPageEventArgs e)
        {
            GraphContext gc = new GraphContext(e.Graphics) { Print = true };
            //gc.G.TranslateTransform(e.MarginBounds.X, e.MarginBounds.Y);

            contextList.recs.Area.Set(0, 0, e.MarginBounds.Width, e.MarginBounds.Height);

            Rectangle rect = new Rectangle();
            rect.Set(e.MarginBounds.X, e.MarginBounds.Y, e.MarginBounds.Width, e.MarginBounds.Height);
            rect.X = 0;

            int last = printpage == 0 ? 0 : contextList.tdIndex.Last + 1;
            var group = contextList.GetRowGroup(last);
            contextList.GetRowBound(last, group, contextList.recs.Row);

            rect.Y = contextList.recs.Row.Top - rect.Y;
            rect.Y -= (contextList._listInfo.ColumnsVisible ? contextList.recs.Columns.Height : 0);
            rect.Y -= (contextList._listInfo.GroupVisible && last == 0 ? contextList._listInfo.GroupHeigh : 0);

            contextList.GetDisplayIndexes(rect);
            contextList.GetRowBound(contextList.tdIndex.Last, contextList.GetRowGroup(contextList.tdIndex.Last), contextList.recs.Row);
            if (contextList.recs.Row.Bottom > rect.Bottom)
                rect.Height -= contextList.recs.Row.Bottom - rect.Bottom;

            contextList.GetColumnsBound();
            contextList.recs.Columns.X += e.MarginBounds.Location.X;
            contextList.recs.Columns.Y += e.MarginBounds.Location.Y;


            contextList.recs.Clip.Set(e.MarginBounds.X, e.MarginBounds.Y, e.MarginBounds.Width, e.MarginBounds.Height);
            contextList.OnPaintList(gc);
            //gc.Dispose();
            printpage++;
            if (contextList.recs.Content.Height > rect.Height * printpage)
                e.HasMorePages = true;
            else
                e.HasMorePages = false;
        }

        private static void PrintBeginPrint(object sender, PrintEventArgs e)
        {
            //e.PrintAction = PrintAction.PrintToPreview;
            //e.PrintAction.
            //throw new NotImplementedException();
        }
#endif
    }
}
