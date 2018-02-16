using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class QField : IInvoker<object[], object>
    {
        public int Index;

        public bool CanWrite { get { return true; } }

        public Type DataType { get; set; }

        public string Name { get; set; }

        public object Get(object[] target)
        {
            return target[Index];
        }

        public object Get(object target)
        {
            return Get((object[])target);
        }

        public void Set(object[] target, object value)
        {
            target[Index] = value;
        }

        public void Set(object target, object value)
        {
            Set((object[])target, value);
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
            if (ColumnsLoaded != null)
                ColumnsLoaded(this, EventArgs.Empty);
        }

        public event EventHandler Loaded;
        public void OnLoaded()
        {
            if (Loaded != null)
                Loaded(this, EventArgs.Empty);
        }

        public int GetIndex(string Column)
        {
            QField value;
            return Columns.TryGetValue(Column, out value) ? value.Index : -1;
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
                    rez = ListHelper.Compare(a[index], b[index], null, false);
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
