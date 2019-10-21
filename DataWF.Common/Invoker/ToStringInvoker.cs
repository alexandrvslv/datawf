using Newtonsoft.Json.Serialization;
using System;

namespace DataWF.Common
{
    public class ToStringInvoker : IInvoker<object, string>
    {
        public static readonly IInvoker<object, string> Instance = new ToStringInvoker();

        public bool CanWrite => false;

        public Type DataType => typeof(string);

        public Type TargetType => typeof(object);

        public string Name { get => nameof(Object.ToString); set { } }

        public string GetValue(object target) => target?.ToString();

        object IValueProvider.GetValue(object target) => GetValue(target);

        public void SetValue(object target, object value) => throw new NotSupportedException();

        public void SetValue(object target, string value) => throw new NotImplementedException();

        public IListIndex CreateIndex(bool concurrent) => ListIndexFabric.Create<object, string>(this, concurrent);
    }
}
