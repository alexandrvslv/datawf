using DataWF.Data.Gui;
using DataWF.Gui;
using System;

namespace DataWF.Starter
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            DataWF.Gui.GuiService.Start(args, Xwt.ToolkitType.Gtk, typeof(Splash), typeof(Main));
        }
    }
}
