using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using DataWF;
using Avalonia.Controls;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.Imaging;
using Avalonia.Input;

namespace DataWF.Gui
{
    public enum PEditorHover
    {
        None,
        DropDown,
        DropDownEx
    }

    public class LayoutEditor : Canvas, ILayoutEditor, INotifyPropertyChanged
    {
        const int w = 17;
        private static Dictionary<string, object> GlobalCache = new Dictionary<string, object>(StringComparer.Ordinal);
        protected Dictionary<string, object> Cache = new Dictionary<string, object>(StringComparer.Ordinal);
        protected ILayoutCellEditor currentEditor;
        protected object value;
        protected Visual control;
        protected bool changed;
        protected bool dropDownVisible = true;
        protected bool dropDownExVisible = true;
        protected PEditorHover hover = PEditorHover.None;
        protected ToolWindow dropDown;
        Bitmap image;
        CellStyle style;
        Rect recte = new Rect();
        Rect rectd = new Rect();
        Rect rectg = new Rect();
        Rect recti = new Rect(0, 0, 18D, 18D);
        ILayoutCell cell;
        CellStyle buttonStyle;

        public LayoutEditor() : base()
        {
        }

        public string ButtonStyleName { get; set; } = "DropDown";

        public CellStyle ButtonStyle
        {
            get => buttonStyle ??= GuiEnvironment.Theme[ButtonStyleName];
            set
            {
                buttonStyle = value;
                ButtonStyleName = value?.Name;
            }
        }

        public ILayoutCell Cell
        {
            get => cell;
            set
            {
                cell = value;
                Style = cell?.Style;
            }
        }

        public bool DropDownAutoHide { get; set; }

        public bool Initialize { get; set; }

        public ILayoutCellEditor CurrentEditor
        {
            get => currentEditor;
            set => currentEditor = value;
        }

        public bool IsValueChanged
        {
            get => changed;
            set
            {
                changed = value;
                Backup = Value;
            }
        }

        public virtual object Value
        {
            get => value;
            set
            {
                if (this.value == value)
                    return;
                this.value = value;
                OnValueChanged();
            }
        }

        public ToolWindow DropDown
        {
            get => dropDown;
            set
            {
                if (dropDown != value)
                {
                    //if (_dropDown != null)
                    //    _dropDown.VisibleChanged -= DropDownFormClosed;
                    dropDown = value;
                    //if (_dropDown != null)
                    //    _dropDown.VisibleChanged += DropDownFormClosed;
                }
            }
        }

        public virtual CellStyle Style
        {
            get => style;
            set
            {
                style = value;
                Background = new ImmutableSolidColorBrush(style?.BaseColor ?? Colors.Transparent);
            }
        }

        public bool DropDownVisible
        {
            get => dropDownVisible;
            set
            {
                if (dropDownVisible != value)
                {
                    dropDownVisible = value;
                    InvalidateArrange();
                    InvalidateVisual();
                }
            }
        }

        public bool DropDownExVisible
        {
            get => dropDownExVisible;
            set
            {
                if (dropDownExVisible != value)
                {
                    dropDownExVisible = value;
                    InvalidateArrange();
                    InvalidateVisual();
                }
            }
        }

        public Bitmap Image
        {
            get => image;
            set
            {
                if (image != value)
                {
                    image = value;
                    InvalidateVisual();
                }
            }
        }

        public Visual Control
        {
            get => control;
            set
            {
                if (value != Control)
                {
                    if (Control != null)
                    {
                        Children.Remove(Control);
                    }
                    Control = value;
                    if (Control != null)
                    {
                        Control.IsVisible = true;
                        Children.Add(Control);
                    }
                }
            }
        }

        public object Backup { get; set; }

        public event EventHandler DropDownClick;

        public event EventHandler DropDownExClick;

        protected void OnDropClick()
        {
            DropDownClick?.Invoke(this, EventArgs.Empty);
            ShowDropDown(ToolShowMode.Default);
        }

        protected void OnDropExClick()
        {
            DropDownExClick?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnValueChanged()
        {
            if (!Initialize)
            {
                changed = true;
                if (DropDown != null && DropDown.IsVisible && DropDownAutoHide)
                    DropDown.Hide();
                //OnPropertyChanged(nameof(Value));
            }
        }

        public void ClearValue()
        {
            value = null;
        }

        public void GetDropDownExtBound()
        {
            int h = w;
            if (dropDownExVisible)
                recte = new Rect(Width - (w + 1), 1, w, h < Height ? h : Height - 2);
            else
                recte = new Rect(0, 0, 0, 0);
        }

        public void GetDropDownBound()
        {
            int h = w;
            GetDropDownExtBound();
            if (dropDownVisible)
            {
                rectd = new Rect(Width - w - (recte.Width + 2), 1, w, h < Height ? h : Height - 2);
            }
            else
            {
                rectd = new Rect(Width - (recte.Width + 2), 0, 0, 0);
            }
        }

        protected virtual Rect GetControlBound()
        {
            return new Rect(image != null ? 19 : 1,
                                 0,
                                 Width - (Width - (int)rectd.Left) - (image != null ? 19 : 1),
                                 Height);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var size = base.MeasureOverride(availableSize);
            size = new Size(
                size.Width + (DropDownVisible ? w : 0) + (DropDownExVisible ? w : 0),
                20);
            //if (Control != null)
            //    size.Width += Control.Measure(availableSize);
            return size;
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            base.ArrangeCore(finalRect);
            GetDropDownBound();
            GetDropDownExtBound();
            if (Control != null)
                Control.Arrange(GetControlBound());
            InvalidateVisual();
        }

        public override void Render(DrawingContext ctx)
        {
            base.Render(ctx);
            var dirtyRect = Bounds;
            using (var context = new GraphContext(ctx))
            {
                if (image != null)
                {
                    context.DrawImage(image, recti);
                }
                if (dropDownVisible)
                {
                    var state = CellDisplayState.Default;
                    if (hover == PEditorHover.DropDown)
                        state = CellDisplayState.Hover;
                    rectg = new Rect(rectd.X, rectd.Y, 16, 16);
                    context.DrawCell(ButtonStyle, GlyphType.AngleDoubleDown, rectd, rectd, state);
                }
                if (dropDownExVisible)
                {
                    var state = CellDisplayState.Default;
                    if (hover == PEditorHover.DropDownEx)
                        state = CellDisplayState.Hover;
                    rectg = new Rect(recte.X, recte.Y, 16, 16);
                    context.DrawCell(ButtonStyle, GlyphType.AngleDoubleRight, recte, recte, state);
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            var position = e.GetPosition(this);
            if (dropDownVisible && rectd.Contains(position))
                OnDropClick();
            if (dropDownExVisible && recte.Contains(position))
                OnDropExClick();
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            var position = e.GetPosition(this);
            if (dropDownVisible && rectd.Contains(position))
            {
                hover = PEditorHover.DropDown;
                InvalidateVisual();
            }
            else if (dropDownExVisible && recte.Contains(position))
            {
                hover = PEditorHover.DropDownEx;
                InvalidateVisual();
            }
            else
            {
                hover = PEditorHover.None;
            }
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            hover = PEditorHover.None;
            InvalidateVisual();
        }

        private void DropDownFormClosed(object sender, EventArgs e)
        {
            if (Control != null && !dropDown.IsVisible)
                Control.Focus();
        }

        protected virtual void Dispose(bool disposing)
        {
            foreach (KeyValuePair<string, object> kv in Cache)
                if (kv.Value is IDisposable)
                    ((IDisposable)kv.Value).Dispose();
            Cache.Clear();
        }

        public void ShowDropDown(ToolShowMode mode)
        {
            if (dropDown != null && !dropDown.IsVisible)
            {
                if (dropDown.Bounds.Width < Bounds.Width && Bounds.Width > 200)
                {
                    dropDown.Width = Bounds.Width;
                }
                if (mode == ToolShowMode.AutoHide)
                    dropDown.TimerInterval = 10000;
                dropDown.Mode = mode;
                dropDown.Show(this, new PixelPoint(0, (int)Bounds.Height));
            }
        }

        public async Task<DialogResult?> ShowDropDownAsync()
        {
            if (dropDown != null && !dropDown.IsVisible)
            {
                if (dropDown.Bounds.Width < Bounds.Width && Bounds.Width > 200)
                    dropDown.Width = Bounds.Width;
                return await dropDown.ShowAsync(this, new PixelPoint(0, (int)Bounds.Height));
            }
            return null;
        }

        public T GetCached<T>()
        {
            return GetCached<T>(typeof(T).Name);
        }

        public T GetCached<T>(string name)
        {
            if (!Cache.TryGetValue(name, out object o))
            {
                Cache[name] = o = InvokeProvider.CreateObject(typeof(T), true);
            }
            return (T)o;
        }

        public T GetGlobalCached<T>()
        {
            var name = typeof(T).Name;
            if (!GlobalCache.TryGetValue(name, out object o))
            {
                GlobalCache[name] = o = InvokeProvider.CreateObject(typeof(T), true);
            }
            return (T)o;
        }
    }
}
