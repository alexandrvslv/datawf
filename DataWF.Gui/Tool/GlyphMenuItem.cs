using DataWF.Common;
using System;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{

    public class GlyphMenuItem : MenuItem
    {
        protected Color color = Colors.Black;
        private GlyphType glyph;

        public event EventHandler DropDownOpened;

        public GlyphMenuItem()
            : base()
        {
        }

        public GlyphMenuItem(EventHandler click)
        {
            Click += click;
        }

        protected override void OnClicked(EventArgs e)
        {
            base.OnClicked(e);
            if (DropDownOpened != null)
                DropDownOpened(this, EventArgs.Empty);
            OnClick();
        }


        public Color ForeColor
        {
            get { return color; }
            set { color = value; }
        }

        public Menu DropDown
        {
            get
            {
                if (SubMenu == null)
                {
                    SubMenu = new Menu();
                }
                return SubMenu;
            }
            set
            {
                if (SubMenu != value)
                {
                    SubMenu = value;
                }
            }
        }
        public bool Checked { get; set; }


        public event EventHandler Click;
        //{
        //     add {base.Activated += value;}
        //     remove { base.Activated -= value;}
        //}

        internal void OnClick()
        {
            if (Click != null)
                Click(this, EventArgs.Empty);
        }

        //protected override void OnActivated()
        //{
        //    base.OnActivated();
        //    Console.WriteLine("Menu Click " + Text);
        //}

        public string Text
        {
            get { return base.Label; }
            set { base.Label = value; }
        }

        public GlyphType Glyph
        {
            get { return glyph; }
            set
            {
                if (glyph != value)
                {
                    glyph = value;
                }
            }
        }

        public GlyphMenuItem Owner { get; set; }

        public Font Font { get; set; }
    }
}
