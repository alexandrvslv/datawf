using DataWF;
using System;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace DataWF.Gui
{
    public class CellEditorGlyph : CellEditorList
    {
        public CellEditorGlyph()
        {
            var list = new SelectableList<GlyphView>();
            foreach (GlyphType enumItem in Enum.GetValues(typeof(GlyphType)))
                list.Add(new GlyphView(enumItem));
            listSource = list;
        }

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            if (value is GlyphView)
                value = ((GlyphView)value).Glyph;
            return base.ParseValue(value, dataSource, valueType);
        }

        private class GlyphView : IGlyph
        {

            public GlyphView(GlyphType glyph)
            {
                Glyph = glyph;
            }

            public override string ToString()
            {
                return Glyph.ToString();
            }

            public GlyphType Glyph { get; set; }

            public Bitmap Image
            {
                get => null;
                set { }
            }
        }
    }

}
