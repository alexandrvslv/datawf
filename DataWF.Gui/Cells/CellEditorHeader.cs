using Xwt;
using DataWF.Common;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class CellEditorHeader : CellEditorText
    {
        public CellEditorHeader()
        {
            DropDownWindow = false;
        }

        public override void DrawCell(LayoutListDrawArgs e)
        {
            var layoutList = e.LayoutList;

            IGroup gitem = e.Item as IGroup;
            IGlyph image = e.Item as IGlyph;
            int level = gitem == null ? -1 : GroupHelper.Level(gitem);

            e.Context.DrawCell(e.Style, null, e.Bound, e.Bound, e.State);

            if (layoutList.ListInfo.Tree && gitem != null && gitem.IsCompaund)
            {
                var glyphBound = layoutList.GetCellGlyphBound(gitem, level, e.Bound);
                e.Context.DrawGlyph(gitem.Expand ? GlyphType.CaretDown : GlyphType.CaretRight, glyphBound, e.Style, e.State);
            }
            if (layoutList.AllowCheck && e.Item is ICheck)
            {
                var checkBound = layoutList.GetCellCheckBound(e.Item, level, e.Bound);
                e.Context.DrawCheckBox(((ICheck)e.Item).Check ? CheckedState.Checked : CheckedState.Unchecked, checkBound, e.Style.FontBrush.GetColorByState(e.State));
            }
            if (layoutList.AllowImage && image != null)
            {
                var imageBound = layoutList.GetCellImageBound(e.Item, level, e.Bound);
                if (image.Image != null)
                {
                    e.Context.DrawImage(image.Image, imageBound);
                }
                else if (image.Glyph != GlyphType.None)
                {
                    var color = e.Style.FontBrush.GetColorByState(e.State);
                    if (image is Node && ((Node)image).GlyphColor != CellStyleBrush.ColorEmpty)
                    {
                        color = ((Node)image).GlyphColor;
                    }
                    e.Context.DrawGlyph(image.Glyph, imageBound, color);
                }
            }
            var textBound = layoutList.GetCellTextBound(e);
            if (e.Formated is string)
                e.Context.DrawText((string)e.Formated, textBound, e.Style, e.State);
            else if (e.Formated is TextLayout)
                e.Context.DrawText((TextLayout)e.Formated, textBound, e.Style, e.State);
        }
    }
}
