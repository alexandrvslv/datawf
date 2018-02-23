using System;
using System.IO;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public static class GuiService
    {
        public static System.Threading.Thread UIThread;

        public static bool InvokeRequired { get { return UIThread != System.Threading.Thread.CurrentThread; } }

        public static MenuItem GetByName(this MenuItemCollection items, string name)
        {
            foreach (var item in items)
            {
                if (item.Name == name)
                    return item;
            }
            return null;
        }

        public static Color Invert(this Color color)
        {
            return new Color(1D - color.Red, 1D - color.Green, 1D - color.Blue, color.Alpha);
        }

        public static Command ShowDialog(this Widget widget, Widget owner)
        {
            return widget.ShowDialog(owner.ParentWindow);
        }

        public static Command ShowDialog(this Widget widget, WindowFrame owner)
        {
            var window = new Dialog { Content = widget };
            if (owner != null)
                window.TransientFor = owner;
            return window.Run(owner);
        }

        public static void ShowWindow(this Widget widget, Widget owner)
        {
            widget.ShowWindow(owner.ParentWindow);
        }

        public static void ShowWindow(this Widget widget, WindowFrame owner)
        {
            var vbox = new VBox();
            vbox.PackStart(widget, true, true);
            var window = new Window
            {
                Content = vbox,
                Resizable = true,
                InitialLocation = WindowLocation.CenterParent,
                Title = widget is IText ? ((IText)widget).Text : widget.Name,
                Padding = new WidgetSpacing(5, 5, 5, 5)
            };
            if (owner != null)
            {
                window.TransientFor = owner;
            }
            window.Visible = true;
            //window.Show();
        }

        public static void Localize(object obj, string category, string name, GlyphType def = GlyphType.None)
        {
            var item = Locale.GetItem(category, name);
            if (item.Glyph == GlyphType.None && def != GlyphType.None)
                item.Glyph = def;
            var picture = obj as IGlyph;
            if (picture != null)
            {
                picture.Image = Locale.GetImage(item.ImageKey) as Image;
                picture.Glyph = item.Glyph;
            }

            var text = TypeHelper.GetMemberInfo(obj.GetType(), "Text");
            if (text != null)
                EmitInvoker.SetValue(text, obj, item.Value);
        }

        public static LayoutAlignType GetAlignRect(Rectangle bound, double size, double x, double y, ref Rectangle rec)
        {
            var sizes = (size + 3);
            LayoutAlignType type = LayoutAlignType.None;
            if (x >= bound.Right - sizes && x <= bound.Right)
            {
                rec.X = bound.Right - sizes;
                rec.Y = bound.Top;
                rec.Width = sizes;
                rec.Height = bound.Height;
                type = LayoutAlignType.Right;
            }
            else if (x <= bound.Left + (size + 2) && x >= bound.Left)
            {
                rec.X = bound.Left;
                rec.Y = bound.Top;
                rec.Width = sizes;
                rec.Height = bound.Height;
                type = LayoutAlignType.Left;
            }
            else if (y <= bound.Top + size && y >= bound.Top)
            {
                rec.X = bound.Left;
                rec.Y = bound.Top;
                rec.Width = bound.Width;
                rec.Height = size;
                type = LayoutAlignType.Top;
            }
            else if (y >= bound.Bottom - size && y <= bound.Bottom)
            {
                rec.X = bound.Left;
                rec.Y = bound.Bottom - size;
                rec.Width = bound.Width;
                rec.Height = size;
                type = LayoutAlignType.Bottom;
            }
            else
                type = LayoutAlignType.None;
            return type;
        }

        public static Image ImageFromBase64(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return null;
                return ImageFromByte(Convert.FromBase64String(text));
            }
            catch (Exception ex)
            {
                Helper.Logs.Add(new StateInfo(ex));
                return null;
            }
        }

        public static Image ImageFromByte(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                return Image.FromStream(stream);
            }
        }

        public static byte[] ImageToByte(Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, ImageFileType.Png);
                return stream.ToArray();
            }
        }

        public static string ImageToBase64(Image img)
        {
            return Convert.ToBase64String(ImageToByte(img));
        }


        public static bool IsCompound(Type type)
        {
            if (type == null)
                return false;
            else if (type.IsValueType)
                return false;
            else if (type == typeof(string))
                return false;
            else if (type == typeof(Image))
                return false;
            else
                return true;
        }

        public static IDockMain Main { get; set; }

        public static Window Wrap(Widget c)
        {
            return new Window() { Content = c };
        }

        private static ToolTipWindow toolTipCache;

        public static ToolTipWindow ToolTip
        {
            get { return toolTipCache ?? (toolTipCache = new ToolTipWindow()); }
        }

        public static IDockContainer GetDockParent(Widget control)
        {
            var c = control.Parent;
            while (c != null)
                if (c is IDockContainer)
                    return (IDockContainer)c;
                else
                    c = c.Parent;
            return null;
        }

        public static IDockContainer GetDockParent(Widget control, string name)
        {
            IDockContainer c = GetDockParent(control);
            while (c != null)
                if (((Widget)c).Name == name)
                    return c;
                else
                    c = GetDockParent((Widget)c);
            return null;
        }
    }
}
