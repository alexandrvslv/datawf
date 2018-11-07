using System;
using Xwt;
using Xwt.Drawing;
using Xwt.Formats;

namespace DataWF.Gui
{
    public class ToolTipControl : Widget
    {
        protected RichTextView content = new RichTextView();

        public ToolTipControl()
            : base()
        {
            InitializeControl();
        }

        public string Text
        {
            get { return content.PlainText; }
            set { content.LoadText(value, new PlainTextFormat()); }
        }

        public void InitializeControl()
        {
            content.ReadOnly = true;
            this.Font = content.Font;
            Content = content;
            BackgroundColor = Colors.LightSlateGray;
        }
    }
}

