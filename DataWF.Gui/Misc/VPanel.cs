using System;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class VPanel : VBox, IText, IGlyph
    {
        private string text;
        private Image image;
        private GlyphType glyph;

        public VPanel()
        {
            Spacing = 1;
        }

        public Image Image
        {
            get { return image; }
            set { image = value; }
        }

        public GlyphType Glyph
        {
            get { return glyph; }
            set { glyph = value; }
        }

        public string Text
        {
            get { return text; }
            set
            {
                if (text != value)
                {
                    text = value;
                    if (TextChanged != null)
                        TextChanged(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler TextChanged;
    }

}
