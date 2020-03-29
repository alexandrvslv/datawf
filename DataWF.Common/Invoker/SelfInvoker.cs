using System;

namespace DataWF.Common
{
    public class SelfInvoker<T> : Invoker<T, T>
    {
        public static readonly SelfInvoker<T> Instance = new SelfInvoker<T>();

        public override bool CanWrite => false;

        public override string Name { get => "this"; }

        public override T GetValue(T target) => target;

        public override void SetValue(T target, T value) { }

    }
}
