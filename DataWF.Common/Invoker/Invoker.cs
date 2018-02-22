using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace DataWF.Common
{
    public class Invoker<T, V> : IInvoker<T, V>
    {
        public Invoker(string name, Func<T, V> getAction, Action<T, V> setAction = null)
        {
            GetAction = getAction;
            SetAction = setAction;
            DataType = typeof(V);
            Name = name;
        }

        public bool CanWrite { get { return SetAction != null; } }

        public string Name { get; set; }

        public Type DataType { get; set; }

        public Type TargetType { get { return typeof(T); } }

        public Func<T, V> GetAction { get; protected set; }

        public Action<T, V> SetAction { get; protected set; }

        public V Get(T target)
        {
            return GetAction(target);
        }

        public object Get(object target)
        {
            Debug.WriteLineIf(target != null && !typeof(T).IsInstanceOfType(target), $"expected {typeof(T)} but get {target.GetType()}");
            return Get((T)target);
        }

        public void Set(T target, V value)
        {
            SetAction(target, value);
        }

        public void Set(object target, object value)
        {
            Set((T)target, (V)value);
        }
    }
}
