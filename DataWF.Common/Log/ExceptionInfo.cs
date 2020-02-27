using System;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace DataWF.Common
{
    public class ExceptionInfo
    {
        public ExceptionInfo(Exception exception)
        {
            Exception = exception;
            Date = DateTime.UtcNow;
            var modules = new StringBuilder();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Array.Sort<Assembly>(assemblies, (x, y) => string.Compare(x.GetName().Name, y.GetName().Name, StringComparison.Ordinal));
            foreach (var a in assemblies)
            {
                var version = a.GetName().Version;
                modules.AppendFormat("{0}: {1}.{2}.{3}\n", a.GetName().Name, version.Major, version.Minor, version.Build);
            }
            Modules = modules.ToString();
        }

        [Category("Data")]
        public string Message
        {
            get { return Exception.Message; }
        }

        [Category("Data")]
        public string Comment { get; set; }

        [Category("EMail")]
        public string Email { get; set; }

        [Category("EMail")]
        public string Subject { get; set; }

        [Category("Misc"), ReadOnly(true)]
        public string Stack
        {
            get { return Exception.StackTrace; }
        }

        [Category("Misc"), ReadOnly(true)]
        public string Help
        {
            get { return Exception.HelpLink; }
        }

        [Category("Misc"), ReadOnly(true)]
        public DateTime Date { get; }

        [Category("Misc"), ReadOnly(true)]
        public string Modules { get; }

        [Category("Data")]
        public Exception Exception { get; }
    }
}
