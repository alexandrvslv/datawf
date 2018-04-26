using Xwt.Drawing;

namespace DataWF.Gui
{
    public class GuiThemeList : NamedList<GuiTheme>
    {
        public void GenerateDefault()
        {
            var dark = new GuiTheme { Name = "Dark" };
            dark.Generate(Font.SystemSansSerifFont, Color.FromBytes(80, 80, 80));
            Add(dark);
            var light = new GuiTheme { Name = "Light" };
            light.Generate(Font.SystemSansSerifFont, Color.FromBytes(190, 190, 190));
            Add(light);
        }
    }


}
