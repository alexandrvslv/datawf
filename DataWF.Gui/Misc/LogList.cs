using DataWF.Gui;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class LogList : LayoutList
    {
        private CellStyle style;
        public LogList()
            : base()
        {
            style = GuiEnvironment.StylesInfo["Logs"];
        }

        protected override void OnDrawHeader(LayoutListDrawArgs e)
        {
            if (e.Item is StateInfo)
            {
                GlyphType type = GlyphType.InfoCircle;
                CellDisplayState gstate = CellDisplayState.Default;
                if (((StateInfo)e.Item).Type == StatusType.Error)
                {
                    type = GlyphType.ExclamationCircle;
                    gstate = CellDisplayState.Selected;
                }
                if (((StateInfo)e.Item).Type == StatusType.Warning)
                {
                    type = GlyphType.ExclamationTriangle;
                    gstate = CellDisplayState.Hover;
                }
                e.Context.DrawGlyph(style, e.Bound, type, gstate);
            }
            else
            {
                base.OnDrawHeader(e);
            }
        }

        protected override void OnListChangedApp(object state)
        {
            base.OnListChangedApp(state);
            if (listSource.Count > 0)
                SelectedItem = listSource[listSource.Count - 1];

        }
    }
}

