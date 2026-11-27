using System;
using DataWF;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;

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
            get => Label.Text;
            set => Label.Text = value;
        }

        public string ContentText
        {
            get => ((ToolTipControl)Target).Text;
            set
            {
                if (((ToolTipControl)Target).Text == value)
                    return;
                ((ToolTipControl)Target).Text = value;
                var s = GraphContext.MeasureString(value, ((ToolTipControl)Target).FontFamily, 640F);
                var h = Padding.Bottom + Padding.Top + s.Height + 30;//(tools.IsVisible ? tools.Height : 0)
                if (h > 600)
                    h = 600;
                var w = Padding.Left + Padding.Right + s.Width + 20;
                if (w > 800)
                    w = 800;
                this.Width = w;
                this.Height = h;
            }
        }

        public void Show(Control c, Point location, string content, string label)
        {
            mode = ToolShowMode.ToolTip;
            ContentText = content;
            LableText = label;
            Show(c, location);
        }
    }
}

