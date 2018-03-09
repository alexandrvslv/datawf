using DataWF.Common;
using System;

namespace DataWF.StarterWpf
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            DataWF.Data.Gui.Main.Start(args, Xwt.ToolkitType.Wpf);
        }
    }
}
