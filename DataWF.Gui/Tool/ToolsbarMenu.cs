using DataWF;
using Avalonia;

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
            get => Editor.Bar;
            set => Editor.Bar = value;
        }
    }
}
