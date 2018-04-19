using DataWF.Gui;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;
using System;

namespace DataWF.Gui
{
    public class LogList : LayoutList
    {
        public LogList() : base()
        { }

        protected override void OnDrawHeader(LayoutListDrawArgs e)
        {
            if (e.Item is StateInfo)
            {
                GlyphType type = GlyphType.InfoCircle;
                Color color = Colors.Blue;
                if (((StateInfo)e.Item).Type == StatusType.Error)
                {
                    type = GlyphType.ExclamationCircle;
                    color = Colors.Red;
                }
                if (((StateInfo)e.Item).Type == StatusType.Warning)
                {
                    type = GlyphType.ExclamationTriangle;
                    color = Colors.Yellow;
                }
                e.Context.DrawGlyph(color, e.Bound, type);
            }
            else
            {
                base.OnDrawHeader(e);
            }
        }

        protected override void OnListChangedApp(object state, EventArgs arg)
        {
            if (listSource.Count > 0)
                SelectedItem = listSource[listSource.Count - 1];
            base.OnListChangedApp(state, arg);
        }
    }
}

