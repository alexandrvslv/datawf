using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class ToolsbarMenu : ToolWindow
    {
        public ToolsbarMenu()
        {
            Target = new ToolsbarEditor();
            Size = new Size(800, 600);
            Title = Locale.Get(nameof(LayoutListMenu), "Toolsbar Constructor");
        }

        public ToolsbarEditor Editor
        {
            get { return (ToolsbarEditor)Target; }
        }

        public Toolsbar ContextBar
        {
            get { return Editor.Bar; }
            set { Editor.Bar = value; }
        }
    }
}
