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
        private bool closing = false;
        private Widget widget;

        public DockPage()
        {
            CarretVisible = true;
            CarretGlyph = GlyphType.CloseAlias;
            CarretStyleName = "PageClose";
            StyleName = "Page";
            DisplayStyle = ToolItemDisplayStyle.ImageAndText;
        }

        public DockPanel Panel
        {
            get { return Bar as DockPanel; }
        }

        public Widget Widget
        {
            get { return widget; }
            set
            {
                if (widget == value)
                    return;

                if (widget is IText)
                    ((IText)widget).TextChanged -= ControlTextChanged;
                widget = value;
                if (widget != null)
                {
                    if (widget is ILocalizable)
                        ((ILocalizable)widget).Localize();
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

        public bool HideOnClose { get; set; }

        public override bool Checked
        {
            get { return base.Checked; }
            set
            {
                if (Checked != value)
                {
                    base.Checked = value;
                    if (value)
                    {
                        UncheckExcept();
                    }
                    Bar?.QueueDraw();
                }
            }
        }

        public void Close()
        {
            if (!closing)
            {
                closing = true;
                if (Panel != null)
                {
                    Panel.ClosePage(this);
                }
                closing = false;
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (!Checked)
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
            foreach (DockPage item in Panel.Items.GetItems())
            {
                if (item != this)
                    item.Checked = false;
            }
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
