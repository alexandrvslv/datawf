using DataWF.Collections.Indexes;
using DataWF.Collections.Query;
using DataWF.Comparers;
using DataWF.Invokers;
using System;
using System.Collections;

namespace DataWF.Collections
{
    public class QField : IIndexInvoker<object[], object, int>, IInvoker<object>, IQueryInvoker<object[], object>
    {
        public int Index { get; set; }

        public bool CanWrite => true;

        public Type DataType { get; set; }

        public Type TargetType => typeof(object[]);

        public string Name { get; set; }

        public object GetValue(in object[] target, int index)
        {
            return target[index];
        }

        public object GetValue(in object[] target)
        {
            return GetValue(target, Index);
        }

        public void SetValue(ref object[] target, object value)
        {
            SetValue(ref target, Index, value);
        }

        public void SetValue(ref object[] target, int index, object value)
        {
            target[index] = value;
        }

        public V GetValue<V>(in object[] target)
        {
            return (V)GetValue(target);
        }

        public void SetValue<V>(ref object[] target, V value)
        {
            SetValue(ref target, Index, value!);
        }
    }
}
