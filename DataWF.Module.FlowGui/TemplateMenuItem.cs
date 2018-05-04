using DataWF.Module.Flow;
using DataWF.Gui;
using DataWF.Common;
using Xwt.Drawing;
using System;

namespace DataWF.Module.FlowGui
{
    public class TemplateMenuItem : ToolMenuItem
    {
        private Template template;

        public TemplateMenuItem(Template template, EventHandler click = null) : base(click)
        {
            Template = template;
        }

        public Template Template
        {
            get { return template; }
            set
            {
                template = value;
                Name = template.Code;
                Glyph = GlyphType.Book;
                Image = (Image)Locale.GetImage("book");
            }
        }

        public override void Localize()
        {
            base.Localize();
            Text = template?.ToString();
        }

    }
}
