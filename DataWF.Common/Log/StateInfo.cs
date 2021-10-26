using DataWF.Common;
using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

[assembly: Invoker(typeof(StateInfo), nameof(StateInfo.User), typeof(StateInfo.UserInvoker))]
[assembly: Invoker(typeof(StateInfo), nameof(StateInfo.Date), typeof(StateInfo.DateInvoker))]
[assembly: Invoker(typeof(StateInfo), nameof(StateInfo.Module), typeof(StateInfo.ModuleInvoker))]
[assembly: Invoker(typeof(StateInfo), nameof(StateInfo.Message), typeof(StateInfo.MessageInvoker))]
[assembly: Invoker(typeof(StateInfo), nameof(StateInfo.Description), typeof(StateInfo.DescriptionInvoker))]
[assembly: Invoker(typeof(StateInfo), nameof(StateInfo.Stack), typeof(StateInfo.StackInvoker))]
[assembly: Invoker(typeof(StateInfo), nameof(StateInfo.Type), typeof(StateInfo.TypeInvoker))]
namespace DataWF.Common
{
    public class StateInfo
    {
        public StateInfo()
        {
            Date = DateTime.UtcNow;
        }

        public StateInfo(Exception e)
            : this("Exeption handler", e.Message, e.GetType().Name, StatusType.Error, e)
        {
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

        public class UserInvoker : Invoker<StateInfo, string>
        {
            public override string Name => nameof(User);

            public override bool CanWrite => true;

            public override string GetValue(StateInfo target) => target.User;

            public override void SetValue(StateInfo target, string value) => target.User = value;
        }

        public class ModuleInvoker : Invoker<StateInfo, string>
        {
            public override string Name => nameof(Module);

            public override bool CanWrite => true;

            public override string GetValue(StateInfo target) => target.Module;

            public override void SetValue(StateInfo target, string value) => target.Module = value;
        }

        public class MessageInvoker : Invoker<StateInfo, string>
        {
            public override string Name => nameof(Message);

            public override bool CanWrite => true;

            public override string GetValue(StateInfo target) => target.Message;

            public override void SetValue(StateInfo target, string value) => target.Message = value;
        }

        public class DescriptionInvoker : Invoker<StateInfo, string>
        {
            public override string Name => nameof(Description);

            public override bool CanWrite => true;

            public override string GetValue(StateInfo target) => target.Description;

            public override void SetValue(StateInfo target, string value) => target.Description = value;
        }

        public class StackInvoker : Invoker<StateInfo, string>
        {
            public override string Name => nameof(Stack);

            public override bool CanWrite => true;

            public override string GetValue(StateInfo target) => target.Stack;

            public override void SetValue(StateInfo target, string value) => target.Stack = value;
        }

        public class TypeInvoker : Invoker<StateInfo, StatusType>
        {
            public override string Name => nameof(Type);

            public override bool CanWrite => true;

            public override StatusType GetValue(StateInfo target) => target.Type;

            public override void SetValue(StateInfo target, StatusType value) => target.Type = value;
        }

        public class DateInvoker : Invoker<StateInfo, DateTime>
        {
            public override string Name => nameof(Date);

            public override bool CanWrite => true;

            public override DateTime GetValue(StateInfo target) => target.Date;

            public override void SetValue(StateInfo target, DateTime value) => target.Date = value;
        }
    }
}
