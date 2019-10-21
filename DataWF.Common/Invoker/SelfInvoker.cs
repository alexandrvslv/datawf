using System;

namespace DataWF.Common
{
    public class SelfInvoker<T> : IInvoker<T, T>
    {
        public static readonly SelfInvoker<T> Instance = new SelfInvoker<T>();

        public bool CanWrite => false;

        public Type DataType => typeof(T);

        public Type TargetType => typeof(T);

        public string Name { get => "this"; set { } }

        public T GetValue(T target) => target;

        public object GetValue(object target) => target;

        public void SetValue(object target, object value) => throw new NotSupportedException();

        public void SetValue(T target, T value) => throw new NotImplementedException();

        public IListIndex CreateIndex(bool concurrent) => ListIndexFabric.Create<T, T>(this, concurrent);
    }

    public class SelfInvoker : SelfInvoker<object>
    {
        public static readonly SelfInvoker Default = new SelfInvoker();
    }
}
