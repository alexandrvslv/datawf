using Newtonsoft.Json.Serialization;
using System;

namespace DataWF.Common
{
    public class ToStringInvoker<T> : Invoker<T, string>
    {
        public static readonly ToStringInvoker<T> Instance = new ToStringInvoker<T>();

        public override string Name => nameof(Object.ToString);

        public override bool CanWrite => false;

        public override string GetValue(T target)
        {
            return target.ToString();
        }

        public override void SetValue(T target, string value)
        { }
    }

    public class ToStringInvoker : ToStringInvoker<object>
    {
        public static readonly ToStringInvoker Default = new ToStringInvoker();

    }
}
