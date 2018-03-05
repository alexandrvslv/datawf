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
                e.Context.DrawGlyph(e.Style, glyphBound, gitem.Expand ? GlyphType.CaretDown : GlyphType.CaretRight, e.State);
            }
            if (layoutList.AllowCheck && e.Item is ICheck)
            {
                var checkBound = layoutList.GetCellCheckBound(e.Item, level, e.Bound);
                e.Context.DrawCheckBox(e.Style.FontBrush.GetColorByState(e.State), checkBound, ((ICheck)e.Item).Check ? CheckedState.Checked : CheckedState.Unchecked);
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
                    e.Context.DrawGlyph(e.Style, imageBound, image.Glyph, e.State);
                }
            }
			var textBound = layoutList.GetCellTextBound(e);
            if (e.Formated is string)
                e.Context.DrawText(e.Style, (string)e.Formated, textBound, e.State);
            else if (e.Formated is TextLayout)
                e.Context.DrawText(e.Style, (TextLayout)e.Formated, textBound, e.State);
        }
    }
}
