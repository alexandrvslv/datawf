using System;

namespace DataWF.Common
{
    public class ActionInvoker<T, V> : Invoker<T, V>
    {
        public ActionInvoker(string name, Func<T, V> getAction, Action<T, V> setAction = null)
        {
            GetAction = getAction;
            SetAction = setAction;
            Name = name;
        }

        public override bool CanWrite { get { return SetAction != null; } }

        public Func<T, V> GetAction { get; protected set; }

        public Action<T, V> SetAction { get; protected set; }

        public override V GetValue(T target)
        {
            return GetAction(target);
        }

        public override void SetValue(T target, V value)
        {
            SetAction(target, value);
        }
    }
}
