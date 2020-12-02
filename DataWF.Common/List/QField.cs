using System;
using System.Collections;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DataWF.Common
{
    public class QField : IIndexInvoker<object[], object, int>, IValuedInvoker<object>
    {
        private JsonEncodedText? jsonName;

        public int Index { get; set; }

        public bool CanWrite { get { return true; } }

        public Type DataType { get; set; }

        public Type TargetType { get { return typeof(object[]); } }

        public string Name { get; set; }

        public JsonEncodedText JsonName { get => jsonName ?? (jsonName = JsonEncodedText.Encode(Name, JavaScriptEncoder.UnsafeRelaxedJsonEscaping)).Value; }

        object IIndexInvoker.Index { get => Index; set => Index = (int)value; }

        public object GetValue(object[] target, int index)
        {
            return target[index];
        }

        public object GetValue(object[] target)
        {
            return GetValue(target, Index);
        }

        public object GetValue(object target, object index)
        {
            return GetValue((object[])target, (int)index);
        }

        public object GetValue(object target)
        {
            return GetValue((object[])target);
        }

        public void SetValue(object[] target, object value)
        {
            SetValue(target, Index, value);
        }

        public void SetValue(object target, object value)
        {
            SetValue((object[])target, value);
        }

        public void SetValue(object[] target, int index, object value)
        {
            target[index] = value;
        }

        public void SetValue(object target, object index, object value)
        {
            SetValue((object[])target, (int)index, value);
        }

        public InvokerComparer CreateComparer()
        {
            return new InvokerComparer<object[], object>(this);
        }

        public IListIndex CreateIndex(bool concurrent)
        {
            return ListIndexFactory.Create<object[], object>(this, concurrent);
        }

        public IQueryParameter CreateParameter()
        {
            return new QueryParameter<object[]>(this);
        }

        public bool CheckItem(object[] item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return ListHelper.CheckItem(GetValue(item), typedValue, comparer, comparision);
        }

        public bool CheckItem(object item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return CheckItem((object[])item, typedValue, comparer, comparision);
        }

        public V GetValue<V>(object target)
        {
            return (V)GetValue(target);
        }

        public void SetValue<V>(object target, V value)
        {
            SetValue(target, value);
        }
    }
}
