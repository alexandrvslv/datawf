using System;

namespace DataWF.Common
{
    public class ActionIndexInvoker<T, V, K> : IndexInvoker<T, V, K>
    {
        private string name;
        internal Func<T, K, V> GetAction { get; private set; }
        internal Action<T, K, V> SetAction { get; private set; }

        public ActionIndexInvoker(string name, Func<T, K, V> getAction, Action<T, K, V> setAction = null)
        {
            GetAction = getAction;
            SetAction = setAction;
            this.name = name;
        }
        public override bool CanWrite => SetAction != null;
        public override string Name => name;

        public override V GetValue(T target, K index)
        {
            return GetAction(target, index);
        }

        public override void SetValue(T target, K index, V value)
        {
            SetAction(target, index, value);
        }
    }
}
