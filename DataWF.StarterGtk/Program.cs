using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using Xwt;
using DataWF.Data.Gui;

namespace DataWF.Starter
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.Initialize(ToolkitType.Gtk);
            GuiService.UIThread = Thread.CurrentThread;
            //exceptions
            Application.UnhandledException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += AppDomainException;

            //Load Configuration
            for (int i = 0; i < args.Length; i++)
            {
                string s = args[i];
                if (s.Equals("-config"))
                {
                    var obj = Serialization.Deserialize(args[++i]);
                    using (var op = new ListExplorer())
                    {
                        op.DataSource = obj;
                        op.ShowWindow((WindowFrame)null);
                    }
                    Application.Run();
                    Serialization.Serialize(obj, args[i]);
                    return;
                }
            }
            using (var login = new Splash())
            {
                login.Run();
            }

            using (var main = new Main())
            {
                main.Show();
                Application.Run();
            }
            Application.Dispose();
        }

        static void OnThreadException(object sender, Xwt.ExceptionEventArgs e)
        {
            Helper.OnException(e.ErrorException);
        }

        private static void AppDomainException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception)
            {
                Helper.Logs.Add(new StateInfo((Exception)e.ExceptionObject));
                Helper.SetDirectory(Environment.SpecialFolder.LocalApplicationData);
                Helper.Logs.Save("crush" + DateTime.Now.ToString("yyMMddhhmmss") + ".log");
            }
        }
    }
}
