using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DataWF.Common
{

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
                    rez = ListHelper.Compare(a[index], b[index], (IComparer)null);
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
