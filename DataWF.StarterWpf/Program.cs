using DataWF.Data.Gui;
using DataWF.Gui;
using System;

namespace DataWF.StarterWin
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            GuiService.Start(args, typeof(Splash), typeof(Main));
        }
    }
}
