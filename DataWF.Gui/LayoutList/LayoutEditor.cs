using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt;
using DataWF.Common;
using Xwt.Drawing;
using System.Threading.Tasks;

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
        protected Widget widget;
        protected bool changed;
        protected bool dropDownVisible = true;
        protected bool dropDownExVisible = true;
        protected PEditorHover hover = PEditorHover.None;
        protected ToolWindow dropDown;
        private Image image;
        private CellStyle style;
        Rectangle recte = new Rectangle();
        Rectangle rectd = new Rectangle();
        Rectangle rectg = new Rectangle();
        Rectangle recti = new Rectangle(0, 0, 18D, 18D);
        private ILayoutCell cell;
        private CellStyle buttonStyle;

        public LayoutEditor() : base()
        {
        }

        public string ButtonStyleName { get; set; } = "DropDown";

        public CellStyle ButtonStyle
        {
            get { return buttonStyle ?? (buttonStyle = GuiEnvironment.Theme[ButtonStyleName]); }
            set
            {
                buttonStyle = value;
                ButtonStyleName = value?.Name;
            }
        }

        public ILayoutCell Cell
        {
            get { return cell; }
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
            get { return currentEditor; }
            set { currentEditor = value; }
        }

        public bool IsValueChanged
        {
            get { return changed; }
            set
            {
                changed = value;
                Backup = Value;
            }
        }

        public virtual object Value
        {
            get { return value; }
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
            get { return dropDown; }
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
            get { return style; }
            set
            {
                style = value;
                BackgroundColor = style?.BaseColor ?? Colors.Transparent;
            }
        }

        public bool DropDownVisible
        {
            get { return dropDownVisible; }
            set
            {
                if (dropDownVisible != value)
                {
                    dropDownVisible = value;
                    OnReallocate();
                    QueueDraw();
                }
            }
        }

        public bool DropDownExVisible
        {
            get { return dropDownExVisible; }
            set
            {
                if (dropDownExVisible != value)
                {
                    dropDownExVisible = value;
                    OnReallocate();
                    QueueDraw();
                }
            }
        }

        public Image Image
        {
            get { return image; }
            set
            {
                if (image != value)
                {
                    image = value;
                    QueueDraw();
                }
            }
        }

        public Widget Widget
        {
            get { return widget; }
            set
            {
                if (value != widget)
                {
                    if (widget != null)
                    {
                        RemoveChild(widget);
                    }
                    widget = value;
                    if (widget != null)
                    {
                        widget.Visible = true;
                        AddChild(widget, GetControlBound());
                    }
                }
            }
        }

        public object Backup { get; set; }

        public event EventHandler DropDownClick;

        public event EventHandler DropDownExClick;

        public event PropertyChangedEventHandler PropertyChanged;

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
                if (DropDown != null && DropDown.Visible && DropDownAutoHide)
                    DropDown.Hide();
                OnPropertyChanged(nameof(Value));
            }
        }

        protected virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public void ClearValue()
        {
            value = null;
        }

        public void GetDropDownExtBound()
        {
            int h = w;
            if (dropDownExVisible)
                recte = new Rectangle(Size.Width - (w + 1), 1, w, h < Size.Height ? h : Size.Height - 2);
            else
                recte = new Rectangle(0, 0, 0, 0);
        }

        public void GetDropDownBound()
        {
            int h = w;
            GetDropDownExtBound();
            if (dropDownVisible)
            {
                rectd = new Rectangle(Size.Width - w - (recte.Width + 2), 1, w, h < Size.Height ? h : Size.Height - 2);
            }
            else
            {
                rectd = new Rectangle(Size.Width - (recte.Width + 2), 0, 0, 0);
            }
        }

        protected virtual Rectangle GetControlBound()
        {
            return new Rectangle(image != null ? 19 : 1,
                                 0,
                                 Size.Width - (Size.Width - (int)rectd.Left) - (image != null ? 19 : 1),
                                 Size.Height);
        }

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            var size = base.OnGetPreferredSize(widthConstraint, heightConstraint);
            size.Height = 20;
            size.Width += DropDownVisible ? w : 0;
            size.Width += DropDownExVisible ? w : 0;
            //if (widget != null)
            //    size.Width += widget.Surface.GetPreferredSize(widthConstraint, heightConstraint).Width;
            return size;
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();
            GetDropDownBound();
            GetDropDownExtBound();
            if (widget != null)
                SetChildBounds(widget, GetControlBound());
            QueueDraw();
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);
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
                    rectg = new Rectangle(rectd.X, rectd.Y, 16, 16);
                    context.DrawCell(ButtonStyle, GlyphType.AngleDoubleDown, rectd, rectd, state);
                }
                if (dropDownExVisible)
                {
                    var state = CellDisplayState.Default;
                    if (hover == PEditorHover.DropDownEx)
                        state = CellDisplayState.Hover;
                    rectg = new Rectangle(recte.X, recte.Y, 16, 16);
                    context.DrawCell(ButtonStyle, GlyphType.AngleDoubleRight, recte, recte, state);
                }
            }
        }

        protected override void OnButtonReleased(ButtonEventArgs e)
        {
            base.OnButtonReleased(e);
            if (dropDownVisible && rectd.Contains(e.Position))
                OnDropClick();
            if (dropDownExVisible && recte.Contains(e.Position))
                OnDropExClick();
        }

        protected override void OnMouseMoved(MouseMovedEventArgs e)
        {
            base.OnMouseMoved(e);
            if (dropDownVisible && rectd.Contains(e.X, e.Y))
            {
                hover = PEditorHover.DropDown;
                QueueDraw();
            }
            else if (dropDownExVisible && recte.Contains(e.X, e.Y))
            {
                hover = PEditorHover.DropDownEx;
                QueueDraw();
            }
            else
            {
                hover = PEditorHover.None;
            }
        }

        protected override void OnMouseExited(EventArgs e)
        {
            base.OnMouseExited(e);
            hover = PEditorHover.None;
            QueueDraw();
        }

        private void DropDownFormClosed(object sender, EventArgs e)
        {
            if (widget != null && !dropDown.Visible)
                widget.SetFocus();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            foreach (KeyValuePair<string, object> kv in Cache)
                if (kv.Value is IDisposable)
                    ((IDisposable)kv.Value).Dispose();
            Cache.Clear();
        }

        public void ShowDropDown(ToolShowMode mode)
        {
            if (dropDown != null && !dropDown.Visible)
            {
                if (dropDown.ScreenBounds.Width < Bounds.Width && Bounds.Width > 200)
                    dropDown.Size = new Size(Bounds.Width, dropDown.ScreenBounds.Height);
                if (mode == ToolShowMode.AutoHide)
                    dropDown.TimerInterval = 10000;
                dropDown.Mode = mode;
                dropDown.Show(this, new Point(0, Bounds.Height));
            }
        }

        public async Task<Command> ShowDropDownAsync()
        {
            if (dropDown != null && !dropDown.Visible)
            {
                if (dropDown.ScreenBounds.Width < Bounds.Width && Bounds.Width > 200)
                    dropDown.Size = new Size(Bounds.Width, dropDown.ScreenBounds.Height);
                return await dropDown.ShowAsync(this, new Point(0, Bounds.Height));
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
                Cache[name] = o = EmitInvoker.CreateObject(typeof(T), true);
            }
            return (T)o;
        }

        public T GetGlobalCached<T>()
        {
            var name = typeof(T).Name;
            if (!GlobalCache.TryGetValue(name, out object o))
            {
                GlobalCache[name] = o = EmitInvoker.CreateObject(typeof(T), true);
            }
            return (T)o;
        }
    }
}
