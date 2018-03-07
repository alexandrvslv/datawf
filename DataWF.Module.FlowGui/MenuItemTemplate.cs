using DataWF.Module.Flow;
using DataWF.Gui;
using DataWF.Common;
using Xwt.Drawing;

namespace DataWF.Module.FlowGui
{
    public class MenuItemTemplate : ToolMenuItem
    {
        private Template template;

        public MenuItemTemplate(Template template)
        {
            this.template = template;
            Text = template.ToString();
            Image = (Image)Locale.GetImage("book");
        }

        public Template Template
        {
            get { return template; }
            set { template = value; }
        }

    }
}
