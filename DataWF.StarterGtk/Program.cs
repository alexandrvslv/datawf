using System;

namespace DataWF.Starter
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            DataWF.Gui.GuiService.Start(args, Xwt.ToolkitType.Gtk);
        }
    }
}
