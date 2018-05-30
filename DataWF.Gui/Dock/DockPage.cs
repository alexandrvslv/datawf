using System;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class DockPage : ToolDropDown
    {
        private bool hideOnClose;
        private bool active;
        private bool closing = false;
        private Widget widget;

        public DockPage()
        {
            CarretVisible = true;
            CarretGlyph = GlyphType.CloseAlias;
            CarretStyleName = "PageClose";
            StyleName = "Page";
            Indent = 2;
            DisplayStyle = ToolItemDisplayStyle.ImageAndText;
        }

        public DockPageBox Box
        {
            get { return Bar as DockPageBox; }
        }

        public DockPanel Panel
        {
            get { return Box?.Panel; }
        }

        public Widget Widget
        {
            get { return widget; }
            set
            {
                if (widget == value)
                    return;
                if (widget != null && widget is IText)
                    ((IText)widget).TextChanged -= ControlTextChanged;
                widget = value;
                if (widget != null)
                {
                    if (widget is IText)
                        ((IText)widget).TextChanged += ControlTextChanged;
                    Text = widget is IText
                        ? ((IText)widget).Text
                                           : string.IsNullOrEmpty(widget.TooltipText)
                                           ? widget.GetType().FullName
                                           : widget.TooltipText; ;
                    if (widget is IGlyph)
                    {
                        Image = ((IGlyph)widget).Image;
                        Glyph = ((IGlyph)widget).Glyph;
                    }
                }
                OnPropertyChanged(nameof(Widget));
            }
        }

        public bool HideOnClose
        {
            get { return hideOnClose; }
            set { hideOnClose = value; }
        }

        public bool Active
        {
            get { return active; }
            set
            {
                if (active != value)
                {
                    active = value;
                    if (active)
                    {
                        UncheckExcept();
                    }
                    Bar?.QueueDraw();
                    OnPropertyChanged(nameof(Active));
                }
            }
        }

        public void Close()
        {
            if (!closing)
            {
                closing = true;
                if (Box != null)
                {
                    Box.ClosePage(this);
                }
                closing = false;
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (!Active)
            {
                Panel.CurrentPage = this;
            }
        }

        public override bool Visible
        {
            get => base.Visible;
            set
            {
                base.Visible = value;

                if (value)
                {
                    Panel.CurrentPage = this;
                }
                else
                {
                    Panel.RemovePage(this);
                }
            }
        }


        protected override void OnCarretClick(ButtonEventArgs args)
        {
            base.OnCarretClick(args);
            Close();
        }


        private void ControlTextChanged(object sender, EventArgs e)
        {
            Text = ((IText)widget).Text;
        }

        private void UncheckExcept()
        {
            foreach (DockPage item in Box.Items.GetItems())
            {
                if (item != this)
                    item.Active = false;
            }
            State = CellDisplayState.Selected;
        }

        public override void Localize()
        {
            //base.Localize();
            if (widget is ILocalizable)
            {
                ((ILocalizable)widget).Localize();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            widget?.Dispose();
            widget = null;
        }
    }
}
