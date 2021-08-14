using DataWF.Common;
using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Common
{
    [InvokerGenerator]
    public partial class StateInfo
    {
        public StateInfo()
        {
            Date = DateTime.UtcNow;
        }

        public StateInfo(Exception e)
            : this()
        {
            Module = e.TargetSite?.ReflectedType?.Name ?? "Exeption handler";
            Message = e.Message;
            Description = e.GetType().Name;
            Type = StatusType.Error; 
            Tag = e;
            Stack = e.StackTrace;
            if (e.InnerException != null)
            {
                Stack += "\n    InnerException: " + e.InnerException.Message + "\n    Stack: " + e.InnerException.StackTrace;
            }
        }

        public StateInfo(string module, string message, string descriprion = null, StatusType type = StatusType.Information, object tag = null)
            : this()
        {
            Module = module;
            Message = message;
            Description = descriprion;
            Type = type;
            Tag = tag;
            //#if TRACE
            //            var stackBuilder = new StringBuilder();
            //            var stack = new System.Diagnostics.StackTrace(true);
            //            foreach (var frame in stack.GetFrames())
            //            {
            //                MethodBase method = frame.GetMethod();
            //                if (method != null && method.DeclaringType != typeof(StateInfo))
            //                {
            //                    stackBuilder.AppendLine($"{(method.DeclaringType?.FullName)}.{method.Name} ({frame.GetFileLineNumber()},{frame.GetFileColumnNumber()}) at {frame.GetFileName()}");
            //                }
            //            }
            //            Stack = stackBuilder.ToString();
            //#endif
        }
        public string User { get; set; }

        public DateTime Date { get; set; }

        public string Module { get; set; }

        public string Message { get; set; }

        [XmlText]
        public string Description { get; set; }

        [XmlText]
        public string Stack { get; set; }

        public StatusType Type { get; set; }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public object Tag { get; set; }
    }
}
