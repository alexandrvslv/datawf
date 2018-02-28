﻿using System;
using System.Collections.Generic;
using Xwt;
using DataWF.Common;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public enum PEditorHover
    {
        None,
        DropDown,
        DropDownEx
    }

    public class LayoutEditor : Canvas, ILayoutEditor
    {
        const int w = 16;
        protected Dictionary<string, object> CacheControls = new Dictionary<string, object>();
        protected ILayoutCellEditor currentEditor;
        protected bool changed;
        protected object value;
        protected Widget widget;
        protected bool dropDownVisible = true;
        protected bool dropDownExVisible = true;
        protected PEditorHover hover = PEditorHover.None;
        protected ToolWindow dropDown;
        private Image image;
        private CellStyle style;
        private CellStyle pstyle;
        Rectangle recte = new Rectangle();
        Rectangle rectd = new Rectangle();
        Rectangle rectg = new Rectangle();
        Rectangle recti = new Rectangle(0, 0, 18D, 18D);

        public LayoutEditor() : base()
        {
            pstyle = GuiEnvironment.StylesInfo["DropDown"];
            BackgroundColor = Colors.White;
        }

        public ILayoutCell Cell { get; set; }

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
            set { changed = value; }
        }

        public virtual object Value
        {
            get { return value; }
            set
            {
                if (this.value == value)
                    return;
                this.value = value;
                OnValueChanged(null);
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

        public CellStyle CellStyle
        {
            get { return style; }
            set
            {
                style = value;
                if (Widget != null)
                {
                    Widget.Font = style.Font;
                }
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

        public event EventHandler DropDownClick;

        public event EventHandler DropDownExClick;

        public event EventHandler ValueChanged;

        protected void OnDropClick()
        {
            DropDownClick?.Invoke(this, EventArgs.Empty);
            ShowDropDown(ToolShowMode.Default);
        }

        protected void OnDropExClick()
        {
            DropDownExClick?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnValueChanged(EventArgs e)
        {
            if (!Initialize)
            {
                changed = true;
                if (DropDown != null && DropDown.Visible && DropDownAutoHide)
                    DropDown.Hide();
                ValueChanged?.Invoke(this, e);
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
                                 Size.Width - (Size.Width - (int)rectd.Left + 1) - (image != null ? 19 : 1),
                                 Size.Height);
        }

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            var size = base.OnGetPreferredSize(widthConstraint, heightConstraint);
            size.Height = 20;
            size.Width += DropDownVisible ? w : 0;
            size.Width += DropDownExVisible ? w : 0;
            if (widget != null)
                size.Width += widget.Surface.GetPreferredSize(widthConstraint, heightConstraint).Width;
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
            GraphContext context = GraphContext.Default;
            context.Context = ctx;

            if (image != null)
            {
                context.DrawImage(image, recti);
            }

            if (dropDownVisible)
            {
                CellDisplayState state = CellDisplayState.Default;
                if (hover == PEditorHover.DropDown)
                    state = CellDisplayState.Hover;
                rectg = new Rectangle(rectd.X, rectd.Y, 16, 16);
                context.DrawCell(pstyle, GlyphType.AngleDoubleDown, rectd, rectd, state);
            }
            if (dropDownExVisible)
            {
                CellDisplayState state = CellDisplayState.Default;
                if (hover == PEditorHover.DropDownEx)
                    state = CellDisplayState.Hover;
                rectg = new Rectangle(recte.X, recte.Y, 16, 16);
                context.DrawCell(pstyle, GlyphType.AngleDoubleRight, recte, recte, state);
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
            foreach (KeyValuePair<string, object> kv in CacheControls)
                if (kv.Value is IDisposable)
                    ((IDisposable)kv.Value).Dispose();
            CacheControls.Clear();
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

        public T GetCacheControl<T>()
        {
            return GetCacheControl<T>(typeof(T).Name);
        }

        public T GetCacheControl<T>(string name)
        {
            object o;
            if (!CacheControls.TryGetValue(name, out o))
            {
                o = EmitInvoker.CreateObject(typeof(T), true);
                CacheControls.Add(name, o);
            }
            return (T)o;
        }
    }
}
