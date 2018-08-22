using DataWF.Common;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class GuiThemeList : NamedList<GuiTheme>
    {
        public void GenerateDefault()
        {
            var dark = new GuiTheme { Name = "Dark" };
            dark.Generate(Font.SystemSansSerifFont, Color.FromBytes(90, 90, 90), Colors.White, -0.06);
            Add(dark);

            var light = new GuiTheme { Name = "Light" };
            light.Generate(Font.SystemSansSerifFont, Color.FromBytes(190, 190, 190), Colors.Black, 0.06);
            Add(light);

            var blue = new GuiTheme() { Name = "Blue" };
            blue.Generate(Font.SystemFont, Color.FromName("#4472c4"), Colors.White, -0.05D);
            //blue["Tool"] = blue.GenerateStyle("Tool", Font.SystemFont.WithSize(Font.SystemFont.Size * 1.08).WithWeight(FontWeight.Semibold), Colors.Red, Colors.White, -0.05D, 5, false, false, false);
            Add(blue);
        }
    }


}
