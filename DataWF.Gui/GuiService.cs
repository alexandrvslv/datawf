using DataWF;
using System;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Dialogs;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DataWF.Gui
{
    public static class GuiService
    {
        public static Thread UIThread;

        public static void Start<App>(string[] args, Type splashType, Type mainType) where App : Application, new()
        {
            if (args.Contains("--wait-for-attach"))
            {
                Console.WriteLine("Attach debugger and use 'Set next statement'");
                while (true)
                {
                    Thread.Sleep(100);
                    if (Debugger.IsAttached)
                        break;
                }
            }
            var builder = BuildAvaloniaApp<App>();
            Application.Initialize(type);
            GuiService.UIThread = Thread.CurrentThread;
            //exceptions
            Application.UnhandledException += (sender, e) =>
            {
                Helper.OnException(e.ErrorException);
            };


            //Load Configuration
            for (int i = 0; i < args.Length; i++)
            {
                string s = args[i];
                if (s.Equals("-config", StringComparison.OrdinalIgnoreCase))
                {
                    var obj = SerializeProvider.Deserialize(args[++i]);
                    var op = new ListExplorer
                    {
                        DataSource = obj
                    };
                    op.ShowWindow((Window)null);
                    Application.Run();
                    SerializeProvider.Serialize(obj, args[i]);
                    return;
                }
            }
            var splash = (Splash)InvokeProvider.CreateObject(splashType);
            splash.Show();
            splash.Closed += Splash_Closed;


            Application.Dispose();
        }

        private static void Splash_Closed(object sender, EventArgs e)
        {
            using (var main = (MainWindow)InvokeProvider.CreateObject(mainType))
            {
                main.LoadConfiguration();
                main.Show();
                Application.Run();
            }
        }

        /// <summary>
        /// This method is needed for IDE previewer infrastructure
        /// </summary>
        public static AppBuilder BuildAvaloniaApp<App>()
            where App : Application, new()
        {
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseSkia()
                .AfterSetup(builder =>
                {
                    
                })
                .LogToTrace();
        }

        public static bool InvokeRequired { get { return UIThread != System.Threading.Thread.CurrentThread; } }

        public static MenuItem GetByName(this IEnumerable<MenuItem> items, string name)
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
            return new Color(1D - color.R, 1D - color.G, 1D - color.B, color.A);
        }

        public static Color WithAlfa(this Color color, byte a)
        {
            return new Color(a, color.R, color.G, color.B);
        }

        public static T GetParent<T>(this Control control) where T : Control
        {
            var parent = control;
            while (parent != null)
            {
                parent = parent.Parent;
                if (parent is T)
                    break;
            }
            return (T)parent;
        }

        public static Task<DialogResult> ShowDialog(this Control Control, Control owner)
        {
            return Control.ShowDialog((Window)owner.GetVisualRoot());
        }

        public static Task<DialogResult> ShowDialog(this Control Control, Window owner)
        {
            if (Control is ILocalizable)
            {
                ((ILocalizable)Control).Localize();
            }
            var window = new Window
            {
                Background = new ImmutableSolidColorBrush(GuiEnvironment.Theme["Window"].BaseColor),
                Content = Control,
                Title = Control.Name,
                Padding = new Thickness(5, 5, 5, 5),
                Width = 800,
                Height = 600,
                Owner = owner
            };
            if (Control is IText)
            {
                window.Title = ((IText)Control).Text;
                ((IText)Control).TextChanged += WindowContentTextChanged;
            }
            window.Closing += (s, e) =>
            {
                //((Dialog)s).Owner = null;
                if (Control is IText)
                {
                    ((IText)Control).TextChanged -= WindowContentTextChanged;
                }
            };
            return window.ShowDialog<DialogResult>(owner);
        }

        public static void ShowWindow(this Control Control, Control owner)
        {
            Control.ShowWindow((Window)owner.GetVisualRoot());
        }

        public static void ShowWindow(this Control Control, Window owner)
        {
            ShowWindow(Control, owner, new Size(800, 600));
        }

        public static void ShowWindow(this Control Control, Window owner, Size size)
        {
            if (Control is ILocalizable)
            {
                ((ILocalizable)Control).Localize();
            }
            var vbox = new StackPanel { Orientation = Orientation.Vertical };
            vbox.Children.Add(Control);
            var window = new Window
            {
                Background = new SolidColorBrush(GuiEnvironment.Theme["Window"].BaseColor),
                Content = vbox,
                CanResize = true,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Title = Control.Name,
                Padding = new Thickness(5, 5, 5, 5),
                Width = size.Width,
                Height = size.Height,
                Owner = owner
            };
            if (Control is IText)
            {
                if (!string.IsNullOrEmpty(((IText)Control).Text))
                {
                    window.Title = ((IText)Control).Text;
                }
                ((IText)Control).TextChanged += WindowContentTextChanged;
            }
            window.Closing += (s, e) =>
            {
                ((Window)s).Owner = null;
                if (Control is IText)
                {
                    ((IText)Control).TextChanged -= WindowContentTextChanged;
                }
            };
            window.Show();
        }

        private static void WindowContentTextChanged(object sender, EventArgs arg)
        {
            ((Window)((Control)sender).GetVisualRoot()).Title = ((IText)sender).Text;
        }

        public static void Localize(object obj, string category, string name, GlyphType def = GlyphType.None)
        {
            var item = Locale.Instance.GetItem(category, name);
            if (item.Glyph == GlyphType.None && def != GlyphType.None)
                item.Glyph = def;
            if (obj is IGlyph picture)
            {
                picture.Image = Locale.GetImage(item.ImageKey) as Bitmap;
                if (item.Glyph != GlyphType.None)
                    picture.Glyph = item.Glyph;
            }

            if (obj is IText text)
            {
                text.Text = item.Value;
            }
        }

        public static LayoutAlignType GetAlignRect(Rect bound, double size, Point point, ref Rect rec)
        {
            var sizes = (size + 3);
            LayoutAlignType type = LayoutAlignType.None;
            if (point.X >= bound.Right - sizes && point.X <= bound.Right)
            {
                rec = new Rect(bound.Right - sizes, bound.Top, sizes, bound.Height);
                type = LayoutAlignType.Right;
            }
            else if (point.X <= bound.Left + (size + 2) && point.X >= bound.Left)
            {
                rec = new Rect(bound.Left, bound.Top, sizes, bound.Height);
                type = LayoutAlignType.Left;
            }
            else if (point.Y <= bound.Top + size && point.Y >= bound.Top)
            {
                rec = new Rect(bound.Left, bound.Top, bound.Width, size);
                type = LayoutAlignType.Top;
            }
            else if (point.Y >= bound.Bottom - size && point.Y <= bound.Bottom)
            {
                rec = new Rect(bound.Left, bound.Bottom - size, bound.Width, size);
                type = LayoutAlignType.Bottom;
            }
            else
                type = LayoutAlignType.None;
            return type;
        }

        public static Bitmap ImageFromBase64(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return null;
                return ImageFromByte(Convert.FromBase64String(text));
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
                return null;
            }
        }

        public static Bitmap ImageFromByte(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                return new Bitmap(stream);
            }
        }

        public static byte[] ImageToByte(Bitmap img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream);
                return stream.ToArray();
            }
        }

        public static string ImageToBase64(Bitmap img)
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

        public static Window Wrap(Control c)
        {
            return new Window() { Content = c };
        }

        private static ToolTipWindow toolTipCache;

        public static ToolTipWindow ToolTip
        {
            get { return toolTipCache ??= new ToolTipWindow(); }
        }

        public static IDockContainer GetDockParent(Control control)
        {
            var c = control.Parent;
            while (c != null)
                if (c is IDockContainer)
                    return (IDockContainer)c;
                else
                    c = c.Parent;
            return null;
        }

        public static IDockContainer GetDockParent(Control control, string name)
        {
            IDockContainer c = GetDockParent(control);
            while (c != null)
                if (((Control)c).Name == name)
                    return c;
                else
                    c = GetDockParent((Control)c);
            return null;
        }
    }
}
