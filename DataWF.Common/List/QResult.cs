using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class QField : IIndexInvoker<object[], object, int>, IValuedInvoker<object>
    {
        public int Index { get; set; }

        public bool CanWrite { get { return true; } }

        public Type DataType { get; set; }

        public Type TargetType { get { return typeof(object[]); } }

        public string Name { get; set; }

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
            return ListIndexFabric.Create<object[], object>(this, concurrent);
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

    public class QResult : IDisposable
    {
        public string Name;
        public Dictionary<string, QField> Columns = new Dictionary<string, QField>(4, StringComparer.OrdinalIgnoreCase);
        public SelectableList<object[]> Values = new SelectableList<object[]>();

        public event EventHandler ColumnsLoaded;

        public void OnColumnsLoaded()
        {
            ColumnsLoaded?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Loaded;

        public void AddField(QField field)
        {
            field.Index = Columns.Count;
            Columns[field.Name] = field;
        }

        public void OnLoaded()
        {
            Loaded?.Invoke(this, EventArgs.Empty);
        }

        public int GetIndex(string Column)
        {
            return Columns.TryGetValue(Column, out QField value) ? value.Index : -1;
        }

        public object Get(int index, string column)
        {
            return Get(Values[index], column);
        }

        public object Get(object[] values, string column)
        {
            var index = GetIndex(column);
            return index >= 0 ? values[index] : null;
        }

        public void Sort(params string[] param)
        {
            Values.Sort((object[] a, object[] b) =>
            {
                int rez = 0;
                foreach (var p in param)
                {
                    int index = GetIndex(p);
                    rez = ListHelper.Compare(a[index], b[index], null);
                    if (rez != 0)
                        return rez;
                }
                return rez;
            });
        }

        public IEnumerable<object[]> Select(string column, CompareType comparer, object value)
        {
            var index = GetIndex(column);
            foreach (var item in Values)
                if (ListHelper.CheckItem(item[index], value, comparer, null))
                    yield return item;
        }

        public void Dispose()
        {
            Columns.Clear();
            ColumnsLoaded = null;
            Values.Dispose();
        }
    }
}
