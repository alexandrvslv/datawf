using DataWF.Common;
using System;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class CellEditorGlyph : CellEditorList
    {
        public CellEditorGlyph()
        {
            var list = new SelectableList<GlyphView>();
            var enumVals = Enum.GetValues(typeof(GlyphType));
            foreach (var enumItem in enumVals)
                list.Add(new GlyphView((GlyphType)enumItem));
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

            public Image Image
            {
                get { return null; }
                set { }
            }
        }
    }

}
