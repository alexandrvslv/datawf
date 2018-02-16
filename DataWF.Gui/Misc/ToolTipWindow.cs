using System;
using DataWF.Common;
using System.ComponentModel;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class ToolTipWindow : ToolWindow
    {
        public ToolTipWindow()
            : base()
        {
            Target = new ToolTipControl();
            //this.TopMost = true;
            mode = ToolShowMode.ToolTip;
            ButtonClose.Visible = false;
            ButtonAccept.Visible = false;

        }

        public string LableText
        {
            get { return Label.Text; }
            set { Label.Text = value; }
        }

        public string ContentText
        {
            get { return ((ToolTipControl)Target).Text; }
            set
            {
                if (((ToolTipControl)Target).Text == value)
                    return;
                ((ToolTipControl)Target).Text = value;
                var s = GraphContext.MeasureString(value, ((ToolTipControl)Target).Font, 640F);
                var h = Padding.Bottom + Padding.Top + s.Height + 30;//(tools.Visible ? tools.Height : 0)
                if (h > 600)
                    h = 600;
                var w = Padding.Left + Padding.Right + s.Width + 20;
                if (w > 800)
                    w = 800;
                this.Width = w;
                this.Height = h;
            }
        }

        public void Show(Widget c, Point location, string content, string label)
        {
            mode = ToolShowMode.ToolTip;
            ContentText = content;
            LableText = label;
            Show(c, location);
        }
    }
}

