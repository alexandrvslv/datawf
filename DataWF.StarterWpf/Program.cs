using DataWF.Data.Gui;
using DataWF.Gui;
using System;

namespace DataWF.StarterWpf
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            GuiService.Start(args, Xwt.ToolkitType.Wpf, typeof(Splash), typeof(Main));
        }
    }
}
