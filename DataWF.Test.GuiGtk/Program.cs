using System;
using System.IO;
using Xwt;
using DataWF.Gui;
using DataWF.Common;

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
            GuiService.Start(args, ToolkitType.Gtk, typeof(Splash), typeof(TestWindow));
        }
    }
}
