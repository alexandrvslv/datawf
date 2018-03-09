using System;

namespace DataWF.Starter
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            DataWF.Data.Gui.Main.Start(args, Xwt.ToolkitType.Gtk);
        }
    }
}
