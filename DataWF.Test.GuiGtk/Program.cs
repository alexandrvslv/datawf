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
        static void Main()
        {
            Application.Initialize(ToolkitType.Gtk);
            using (var window = new FormTest())
            {
                window.Show();
                Application.Run();
            }
            Application.Dispose();
        }
    }
}
