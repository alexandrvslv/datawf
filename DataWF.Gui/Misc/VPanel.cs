using System;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class VPanel : VBox, IText, IGlyph, ILocalizable
    {
        private string text;

        public VPanel()
        {
            Spacing = 1;
        }

        public Image Image { get; set; }

        public GlyphType Glyph { get; set; }

        public string Text
        {
            get { return text; }
            set
            {
                if (text != value)
                {
                    text = value;
                    TextChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler TextChanged;

        public virtual void Localize()
        {
            foreach (var widget in Children)
            {
                if (widget is ILocalizable)
                {
                    ((ILocalizable)widget).Localize();
                }
            }
        }
    }

}
