using System;
using System.IO;
using Avalonia;
using DataWF.Gui;
using DataWF;

namespace DataWF.TestGui
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            GuiService.Start(args, ToolkitType.Wpf, typeof(Splash), typeof(TestWindow));
        }
    }
}
