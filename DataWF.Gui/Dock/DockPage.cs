using System;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace DataWF.Gui
{
    public class DockPage : ToolCarret
    {
        private bool closing = false;
        private IControl control;

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

        [XmlIgnore]
        public override Toolsbar Bar
        {
            get => base.Bar;
            set
            {
                if (Bar == value)
                    return;
                if (Bar != null)
                {
                    Panel.RemovePage(this);
                }
                base.Bar = value;
                if (Bar != null && Control != null && Visible && Checked)
                {
                    Panel.CurrentPage = this;
                }
            }
        }

        public IControl Control
        {
            get => Control;
            set
            {
                if (Control == value)
                    return;

                if (Control is IText)
                    ((IText)Control).TextChanged -= ControlTextChanged;
                Control = value;
                if (Control != null)
                {
                    if (Control.Name != Name)
                    {
                        Control.Name = Name;
                    }
                    if (Control is ILocalizable)
                        ((ILocalizable)Control).Localize();
                    if (Control is IText)
                        ((IText)Control).TextChanged += ControlTextChanged;
                    Text = Control is IText
                        ? ((IText)Control).Text
                                           : string.IsNullOrEmpty(Control.Tooltip)
                                           ? Control.GetType().FullName
                                           : Control.TooltipText;
                    if (Control is IGlyph)
                    {
                        Image = ((IGlyph)Control).Image;
                        Glyph = ((IGlyph)Control).Glyph;
                    }
                }
                OnPropertyChanged(nameof(Control));
            }
        }

        public bool HideOnClose { get { return Control is IDockContent ? ((IDockContent)Control).HideOnClose : false; } }

        public override bool Checked
        {
            get => base.Checked;
            set
            {
                if (value)
                {
                    UncheckExcept();
                }
                if (Checked != value)
                {
                    base.Checked = value;

                    Bar?.InvalidateVisual();
                }
            }
        }

        public void Close()
        {
            if (!closing)
            {
                closing = true;
                Panel?.OnPageClose(this);
                closing = false;
            }
        }

        public override bool Visible
        {
            get => base.Visible;
            set
            {
                if (Visible == value)
                    return;
                base.Visible = value;
                if (Panel != null)
                {
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
        }

        protected override void OnCarretClick(PointerEventArgs args)
        {
            base.OnCarretClick(args);
            Close();
        }


        private void ControlTextChanged(object sender, EventArgs e)
        {
            Text = ((IText)Control).Text;
        }

        private void UncheckExcept()
        {
            if (Map == null)
                return;
            foreach (DockPage item in Map.GetItems())
            {
                if (item != this)
                    item.Checked = false;
            }
        }

        public override void Localize()
        {
            //base.Localize();
            if (Control is ILocalizable)
            {
                ((ILocalizable)Control).Localize();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            Control = null;
        }
    }
}
