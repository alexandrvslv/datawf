using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace DataWF.Common
{

    public static class ListHelper
    {
        public static IList Create(Type type, int capacity)
        {
            return (IList)EmitInvoker.CreateObject(type, new Type[] { typeof(int) }, new object[] { capacity }, true);
        }

        public static IList Copy(IList a)
        {
            return Copy(a, 0, a.Count - 1);
        }

        public static IList Copy(IList a, int start, int stop)
        {
            IList temp = Create(a.GetType(), (stop - start) + 1);
            for (int i = start; i <= stop; i++)
                temp.Add(a[i]);
            return temp;
        }

        public static IList AND(IList consta, IList constb, IComparer comp)
        {
            IList a = consta;
            IList b = constb;
            IList temp = a;
            if (a.Count > b.Count)
            {
                a = b;
                b = temp;
            }
            temp = Create(a.GetType(), a.Count);
            if (a.Count > 0)
            {
                QuickSort(a, comp);
                QuickSort(b, comp);
                int jb = 0, je = b.Count - 1;
                for (int i = 0; i < a.Count; i++)
                {
                    var index = BinarySearch(b, jb, je, a[i], comp);
                    if (index >= 0)
                    {
                        jb = index;
                        temp.Add(a[i]);
                    }
                    else
                    {
                        jb = (-index) - 1;
                    }
                }
            }
            return temp;
        }

        public static IList OR(IList consta, IList constb, IComparer comp)
        {
            IList a = consta;
            IList b = constb;
            IList temp = a;
            if (a.Count > b.Count)
            {
                a = b;
                b = temp;
            }
            QuickSort(b, null);
            temp = Copy(b);

            for (int i = 0; i < a.Count; i++)
                if (BinarySearch(b, a[i], comp) < 0)
                    temp.Add(a[i]);
            return temp;
        }

        public static IList ORNOT(IList consta, IList constb, IComparer comp)
        {
            IList a = consta;
            IList b = constb;
            IList temp = a;
            if (a.Count > b.Count)
            {
                a = b;
                b = temp;
            }

            QuickSort(a, null);
            //temp = Create(a.GetType(), a.Count);
            temp = Copy(b);

            for (int i = b.Count - 1; i >= 0; i--)
                if (BinarySearch(a, b[i], comp) >= 0)
                    temp.RemoveAt(i);
            return temp;
        }

        public static IList ANDNOT(IList consta, IList constb, IComparer comp)
        {
            IList a = consta;
            IList b = constb;
            IList temp = a;
            if (a.Count > b.Count)
            {
                a = b;
                b = temp;
            }

            QuickSort(a, null);
            //temp = Create(a.GetType(), a.Count);
            temp = Copy(b);

            for (int i = b.Count - 1; i >= 0; i--)
                if (BinarySearch(a, b[i], comp) >= 0)
                    temp.RemoveAt(i);
            return temp;
        }

        public static void ADD(IList consta, IList constb)
        {
            for (int i = 0; i < constb.Count; i++)
                consta.Add(constb[i]);
            //BinaryInsert(consta, constb[i], comp);
        }

        public static void ADD<T>(IList<T> consta, IList<T> constb)
        {
            for (int i = 0; i < constb.Count; i++)
                consta.Add(constb[i]);
            //BinaryInsert(consta, constb[i], comp);
        }


        public static int Insert(IList array, object value, IComparer comp)
        {
            int index = BinarySearch(array, 0, array.Count - 1, value, comp);
            if (index < 0)
                index = (-index) - 1;
            if (index > array.Count)
                index = array.Count;
            array.Insert(index, value);
            return index;
        }

        public static int BinarySearch<T>(IList<T> array, T value, IComparer<T> comp)
        {
            return BinarySearch<T>(array, 0, array.Count - 1, value, comp);
        }

        public static int BinarySearch<T>(IList<T> array, int low, int high, T value, IComparer<T> comp)
        {
            int midpoint = 0;
            int rez = 0;
            while (low <= high)
            {
                midpoint = low + ((high - low) >> 1);

                // check to see if value is equal to item in array
                rez = CompareT<T>(value, array[midpoint], comp, true);
                if (rez == 0)//check 
                    if (midpoint > 0 && CompareT<T>(value, array[midpoint - 1], comp, true) <= 0)
                        rez = -1;
                    else if (midpoint < array.Count - 1 && CompareT<T>(value, array[midpoint + 1], comp, true) >= 0)
                        rez = 1;
                if (rez == 0)
                    return midpoint;
                if (rez < 0)
                    high = midpoint - 1;
                else
                    low = midpoint + 1;
            }
            // item was not found
            return -low - 1;
        }

        public static int BinarySearch(IList array, object value, IComparer comp)
        {
            return BinarySearch(array, 0, array.Count - 1, value, comp);
        }

        //Binary search finds item in sorted array.
        // And returns index (zero based) of item
        // If item is not found returns -index
        // Based on C++ example at
        // http://en.wikibooks.org/wiki/Algorithm_implementation/Search/Binary_search#C.2B.2B_.28common_Algorithm.29
        // http://codelab.ru/task/binsearch/
        public static int BinarySearch(IList array, int low, int high, object value, IComparer comp)
        {
            int midpoint = 0;
            int rez = 0;
            while (low <= high)
            {
                midpoint = (low + high) >> 1;

                // check to see if value is equal to item in array
                rez = Compare(value, array[midpoint], comp, true);

                if (rez == 0)//check 
                    if (midpoint > 0 && Compare(value, array[midpoint - 1], comp, true) <= 0)
                        rez = -1;
                    else if (midpoint < array.Count - 1 && Compare(value, array[midpoint + 1], comp, true) >= 0)
                        rez = 1;

                if (rez == 0)
                    return midpoint;
                if (rez < 0)
                    high = midpoint - 1;
                else
                    low = midpoint + 1;
            }
            // item was not found
            return -low - 1;
        }

        public static IEnumerable<T> TypeOf<T>(this IEnumerable enumerable)
        {
            if (enumerable is IEnumerable<T> already)
                return already;
            return TypeOfInternal<T>(enumerable);
        }

        public static IEnumerable<T> TypeOfInternal<T>(IEnumerable enumerable)
        {
            if (enumerable == null)
                yield break;
            foreach (var item in enumerable)
            {
                if (item is T typed)
                    yield return typed;
            }
        }

        public static bool CheckItem(object x, object y, CompareType compare, IComparer comparer)
        {
            bool result = false;
            if (compare.Type == CompareTypes.Equal)
            {
                result = Equal(x, y, false) ? !compare.Not : compare.Not;
            }
            else if (compare.Type == CompareTypes.Is)
            {
                result = compare.Not ? (x != null && x != DBNull.Value) : (x == null || x == DBNull.Value);
            }
            else if (x != null && y != null && compare.Type == CompareTypes.Like)
            {
                y = y.ToString().Trim(new char[] { '%' });
                if (x.ToString().IndexOf((string)y, 0, StringComparison.OrdinalIgnoreCase) >= 0)
                    result = !compare.Not;
                else
                    result = compare.Not;
            }
            else if (compare.Type == CompareTypes.In)
            {
                object val = x;
                if (y is string)
                {
                    y = ((string)y).Split(',');
                }
                if (y is IList list)
                {
                    foreach (object item in list)
                    {
                        if (item is string && !(val is string))
                            val = x == null ? string.Empty : x.ToString();
                        if (item.Equals(val))
                        {
                            result = true;
                            break;
                        }
                    }
                    if (compare.Not)
                        result = !result;
                }
                else if (y is Enum && x is Enum)
                {
                    result = ((int)y & (int)x) != 0;

                    if (compare.Not)
                        result = !result;
                }
            }
            else
            {
                int i = Compare(x, y, comparer, false);
                switch (compare.Type)
                {
                    case CompareTypes.Greater:
                        result = i > 0;
                        break;
                    case CompareTypes.Less:
                        result = i < 0;
                        break;
                    case CompareTypes.GreaterOrEqual:
                        result = i >= 0;
                        break;
                    case CompareTypes.LessOrEqual:
                        result = i <= 0;
                        break;
                }
            }
            return result;
        }

        public static IFilterable GetListView(object dataSource)
        {
            throw new NotImplementedException();
            //dataSource as ;
            //if (filterable == null && dataSource is ISelectable)
            //    filterable = ((ISelectable)dataSource).
        }

        public static IEnumerable Search(IEnumerable items, IQueryParameter param)
        {
            return Search(items, param.Invoker, param.TypedValue, param.Comparer, param.Comparision);
        }

        public static IEnumerable Search(IEnumerable items, IInvoker invoker, object value, CompareType compare, IComparer comparer)
        {
            foreach (var item in items)
            {
                if (CheckItem(invoker.GetValue(item), value, compare, comparer))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<T> Search<T>(IEnumerable<T> items, QueryParameter<T> param)
        {
            foreach (var item in items)
            {
                if (item != null && CheckItem(param.Invoker.GetValue(item), param.TypedValue, param.Comparer, param.Comparision))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<T> Select<T>(IEnumerable<T> items, Query<T> query, ListIndexes<T> indexes = null)
        {
            IEnumerable<T> buffer = items;
            var stack = new Stack<SelectStackEntry<T>>(0);
            bool? flag = null;
            foreach (var parameter in query.GetEnabled())
            {
                var curParameter = parameter;
                var temp = Select<T>(items, curParameter, indexes);
                if (curParameter.GroupBegin)
                {
                    stack.Push(new SelectStackEntry<T>() { Buffer = temp, Parameter = curParameter });
                    continue;
                }
                else if (stack.Count > 0)
                {
                    var entry = stack.Pop();
                    entry.Buffer = Concat(entry.Buffer, temp, curParameter);
                    if (curParameter.GroupEnd)
                    {
                        temp = entry.Buffer;
                        curParameter = entry.Parameter;
                    }
                    else
                    {
                        stack.Push(entry);
                        continue;
                    }
                }
                if (flag == null)
                {
                    buffer = temp;
                    flag = true;
                }
                else
                {
                    buffer = Concat(buffer, temp, curParameter);
                }
            }
            return buffer;
        }

        public static IEnumerable<T> Concat<T>(IEnumerable<T> buffer, IEnumerable<T> temp, IQueryParameter parameter)
        {
            switch (parameter.Logic.Type)
            {
                case LogicTypes.Or:
                    return parameter.Logic.Not
                    ? buffer.Except(temp)
                    : buffer.Union(temp);
                case LogicTypes.And:
                    return parameter.Logic.Not
                   ? buffer.Except(temp).Union(temp.Except(buffer))
                   : buffer.Intersect(temp);
                default:
                    return buffer.Concat(temp);
            }
        }

        public static IEnumerable<T> Select<T>(IEnumerable<T> items, QueryParameter<T> param, ListIndexes<T> indexes = null)
        {
            var index = indexes?.GetIndex(param.Name);
            if (index != null)
            {
                return index.Scan(param);
            }
            return Search<T>(items, param);
        }

        public static IEnumerable Select(IEnumerable items, IQuery query, IListIndexes indexes = null)
        {
            var buffer = items.OfType<object>();
            bool? flag = null;
            foreach (var parameter in query.Parameters.Where(p => p.IsEnabled))
            {
                var temp = Select(items, parameter, indexes).OfType<object>();
                if (flag == null)
                {
                    buffer = temp;
                    flag = true;
                }
                else
                {
                    buffer = Concat(buffer, temp, parameter);
                }
            }
            return buffer;
        }

        public static IEnumerable Select(IEnumerable items, IQueryParameter param, IListIndexes indexes = null)
        {
            IListIndex index = indexes?.GetIndex(param.Name);
            if (index == null)
            {
                return index.Scan(param);
            }
            else
            {
                return Search(items, param);
            }
        }

        public struct SelectStackEntry<T>
        {
            public IEnumerable<T> Buffer;
            public QueryParameter<T> Parameter;
        }

        public struct CheckStackEntry
        {
            public bool Flag;
            public IQueryParameter Parameter;
        }

        public static bool CheckItem(object item, IQuery checkers)
        {
            bool? flag = null;
            var stack = new Stack<CheckStackEntry>(0);
            foreach (var parameter in checkers.Parameters.Where(p => p.IsEnabled))
            {
                bool rez = CheckItem(parameter.Invoker.GetValue(item), parameter.TypedValue, parameter.Comparer, parameter.Comparision);
                var currParameter = parameter;
                if (currParameter.GroupBegin)
                {
                    stack.Push(new CheckStackEntry { Flag = rez, Parameter = currParameter });
                    continue;
                }
                else if (stack.Count > 0)
                {
                    var entry = stack.Pop();
                    entry.Flag = Concat(entry.Flag, rez, currParameter);

                    if (currParameter.GroupEnd)
                    {
                        rez = entry.Flag;
                        currParameter = entry.Parameter;
                    }
                    else
                    {
                        stack.Push(entry);
                        continue;
                    }
                }
                if (flag == null)
                {
                    flag = rez;
                }
                else
                {
                    flag = Concat(flag.Value, rez, currParameter);
                }
            }
            return flag ?? true;
        }

        private static bool Concat(bool flag, bool rez, IQueryParameter parameter)
        {
            switch (parameter.Logic.Type)
            {
                case LogicTypes.Or:
                    return parameter.Logic.Not ? flag | !rez : flag | rez;
                case LogicTypes.And:
                    return parameter.Logic.Not ? flag & !rez : flag & rez;
                default:
                    return flag | rez;
            }
        }

        private static void Swap(IList array, int Left, int Right)
        {
            object tempObj = array[Right];
            array[Right] = array[Left];
            array[Left] = tempObj;
        }

        public static void QuickSort<T>(IList<T> array, IComparer<T> comp)
        {
            if (array.Count > 1)
            {
                QuickSort1<T>(array, 0, array.Count - 1, comp);
            }
        }
        /// <summary>
        /// List sort. Implementation of quick sork algorithm
        /// </summary>
        public static void QuickSort(IList array, IComparer comp)
        {
            //System.Console.Out.WriteLine("QuickSort");
            //intcomp c = new intcomp();

            //ArrayList a = GetArray();
            //long time = Environment.TickCount;
            //a.Sort(c);
            //time = Environment.TickCount - time;
            //Console.WriteLine("Time Elapsed standart: " + time + " msecs.");
            //time = Environment.TickCount;
            //a.Sort(c);
            //time = Environment.TickCount - time;
            //Console.WriteLine("S Time Elapsed standart: " + time + " msecs.");

            //a = GetArray();
            //time = Environment.TickCount;
            //QuickSort2(a, 0, a.Count - 1, c);
            //time = Environment.TickCount - time;
            //Console.WriteLine("Time Elapsed osix: " + time + " msecs.");

            //time = Environment.TickCount;
            //QuickSort2(a, 0, a.Count - 1, c);
            //time = Environment.TickCount - time;
            //Console.WriteLine("S Time Elapsed osix: " + time + " msecs.");

            //a = GetArray();
            //time = Environment.TickCount;
            //QuickSort1(a, 0, a.Count - 1, c);
            //time = Environment.TickCount - time;
            //Console.WriteLine("Time Elapsed this: " + time + " msecs.");

            //time = Environment.TickCount;
            //QuickSort1(a, 0, a.Count - 1, c);
            //time = Environment.TickCount - time;
            //Console.WriteLine("S Time Elapsed this: " + time + " msecs.");
            if (array.Count > 1)
                QuickSort1(array, 0, array.Count - 1, comp);
            //lock (sorting)
            //{
            //    sorting.Remove(array);
            //}
        }

        public static void QuickSort1<T>(IList<T> a, int low, int high, IComparer<T> comp)
        {
            int i = low, j = high;
            T x = a[(low + high) >> 1];

            for (; i <= j; i++, j--)
            {
                while (CompareT<T>(a[i], x, comp, true) < 0) ++i;
                while (CompareT<T>(a[j], x, comp, true) > 0) --j;
                if (i > j)
                    break;
                if (i < j)
                {
                    T temp = a[j]; a[j] = a[i]; a[i] = temp;
                }
            }

            if (a.Count > 200000 && low == 0 && high == a.Count - 1 && Environment.ProcessorCount > 1 && low < j && i < high)
            {
                Parallel.Invoke(() => { QuickSort1(a, low, j, comp); }, () => { QuickSort1(a, i, high, comp); });
            }
            else
            {
                if (low < j)
                    if (j - low < 20)
                        LinearSort<T>(a, low, j, comp);
                    else
                        QuickSort1<T>(a, low, j, comp);
                if (i < high)
                    if (high - i < 20)
                        LinearSort<T>(a, i, high, comp);
                    else
                        QuickSort1<T>(a, i, high, comp);
            }
        }

        //wiki
        public static void QuickSort1(IList a, int low, int high, IComparer comp)
        {
            int i = low, j = high;
            object x = a[(low + high) >> 1];

            for (; i <= j; i++, j--)
            {
                while (Compare(a[i], x, comp, true) < 0) ++i;
                while (Compare(a[j], x, comp, true) > 0) --j;
                if (i > j)
                    break;
                if (i < j)
                {
                    object temp = a[j]; a[j] = a[i]; a[i] = temp;
                }
            }

            if (a.Count > 200000 && low == 0 && high == a.Count - 1 && Environment.ProcessorCount > 1 && low < j && i < high)
            {
                Parallel.Invoke(() => { QuickSort1(a, low, j, comp); }, () => { QuickSort1(a, i, high, comp); });
                //var args1 = QuickSortAsynch(a, low, j, comp);
                //var args2 = QuickSortAsynch(a, i, high, comp);
                //args1.Result.AsyncWaitHandle.WaitOne();
                //args2.Result.AsyncWaitHandle.WaitOne();
            }
            else
            {
                if (low < j)
                {
                    if (j - low < 20)
                        LinearSort(a, low, j, comp);
                    else
                        QuickSort1(a, low, j, comp);
                }
                if (i < high)
                {
                    if (high - i < 20)
                        LinearSort(a, i, high, comp);
                    else
                        QuickSort1(a, i, high, comp);
                }
            }
        }

        //http://codelab.ru/task/8/
        public static void LinearSort<T>(IList<T> x, int s, int n, IComparer<T> comp)
        {
            int i, j;
            n = n + 1;
            for (i = s + 1; i < n; i++)
            {
                T t = x[i];
                for (j = i; j > s && CompareT(x[j - 1], t, comp, true) > 0; j--)
                    x[j] = x[j - 1];

                x[j] = t;
            }
        }

        public static void LinearSort(IList x, int s, int n, IComparer comp)
        {
            int i, j;
            n = n + 1;
            for (i = s + 1; i < n; i++)
            {
                object t = x[i];
                for (j = i; j > s && Compare(x[j - 1], t, comp, true) > 0; j--)
                    x[j] = x[j - 1];

                x[j] = t;
            }
        }

        public static QuickSortEventArgs QuickSortAsynch(IList array, int min, int max, IComparer comp)
        {
            var args = new QuickSortEventArgs { List = array, Min = min, Max = max, Comparer = comp };
            QuickSortAsynch(args);
            return args;
        }

        public static IAsyncResult QuickSortAsynch(QuickSortEventArgs arg)
        {
            arg.Result = action.BeginInvoke(arg.List, arg.Min, arg.Max, arg.Comparer, callback, arg);
            return arg.Result;
        }

        public static event Action<QuickSortEventArgs> QuickSortFinished;
        private static readonly AsyncCallback callback = new AsyncCallback(QuickSortFinish);
        private static Action<IList, int, int, IComparer> action = new Action<IList, int, int, IComparer>(QuickSort1);

        public static void QuickSortFinish(IAsyncResult result)
        {
            action.EndInvoke(result);
            var arg = (QuickSortEventArgs)result.AsyncState;
            QuickSortFinished?.Invoke(arg);
        }

        public static void QuickSortCancel()
        {
            //TODO
        }

        public static bool Equal(object x, object y, bool hash)
        {
            if (x == null || x == DBNull.Value)
            {
                return y == null || y == DBNull.Value;
            }
            else if (y == null || y == DBNull.Value)
            {
                return false;
            }
            bool result = false;

            if (x is Enum || y is Enum)
            {
                x = (int)x;
                y = (int)y;
            }
            if (x is string xString)
            {
                result = xString.Equals(y.ToString(), StringComparison.OrdinalIgnoreCase);
            }
            else if (y is string yString)
            {
                result = yString.Equals(x.ToString(), StringComparison.OrdinalIgnoreCase);
            }
            else if (x.Equals(y))
            {
                result = true;
            }
            if (!result && hash)
            {
                result = x.GetHashCode().Equals(y.GetHashCode());
            }

            return result;
        }

        public static int Compare(object x, object y, IComparer comp, ListSortDirection direction, bool hash)
        {
            return direction == ListSortDirection.Ascending ? Compare(x, y, comp, hash) : -Compare(x, y, comp, hash);
        }

        public static int CompareT<T>(T x, T y, IComparer<T> comp, bool hash)
        {
            int result = 0;
            if (comp != null)
                result = comp.Compare(x, y);
            else if (x == null || DBNull.Value.Equals(x))
            {
                result = (y == null || DBNull.Value.Equals(y)) ? 0 : -1;
                hash = false;
            }
            else if (y == null || DBNull.Value.Equals(y))
            {
                result = 1;
                hash = false;
            }
            else if (x.Equals(y))
            {
                result = 0;
                hash = false;
            }
            else if (x is string)
            {
                result = string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
                hash = false;
            }
            else if (x is DateTime)
            {
                var xd = (DateTime)(object)x;
                var yd = (DateTime)(object)y;
                if (xd.TimeOfDay == TimeSpan.Zero || yd.TimeOfDay == TimeSpan.Zero)
                    result = xd.Date.CompareTo(yd.Date);
                else
                    result = xd.CompareTo(yd);
            }
            else if (x is IComparable<T>)
                result = ((IComparable<T>)x).CompareTo(y);
            else if (x is IComparable)
                result = ((IComparable)x).CompareTo(y);
            else if (x is byte[])
                result = ((byte[])(object)x).Length.CompareTo(((byte[])(object)y).Length);
            else
                result = string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);

            if (hash && result == 0)
                result = x.GetHashCode().CompareTo(y.GetHashCode());
            return result;
        }

        public static int Compare(object x, object y, IComparer comp, bool hash)
        {
            int result = 0;
            if (comp != null)
                result = comp.Compare(x, y);
            else if (x == null || x == DBNull.Value)
            {
                result = (y == null || y == DBNull.Value) ? 0 : -1;
                hash = false;
            }
            else if (y == null || y == DBNull.Value)
            {
                result = 1;
                hash = false;
            }
            else if (x.Equals(y))
            {
                result = 0;
                hash = false;
            }
            else if (x is string)
            {
                result = string.Compare((string)x, y.ToString(), StringComparison.OrdinalIgnoreCase);
                hash = false;
            }
            else if (x is DateTime && y is DateTime)
            {
                var xd = (DateTime)x;
                var yd = (DateTime)y;
                if (xd.TimeOfDay == TimeSpan.Zero || yd.TimeOfDay == TimeSpan.Zero)
                    result = xd.Date.CompareTo(yd.Date);
                else
                    result = xd.CompareTo(yd);
            }
            else if (x is IComparable)//x.GetType() == y.GetType() &&
                result = ((IComparable)x).CompareTo(y);
            else if (x.GetType() == typeof(byte[]))
                result = ((byte[])x).Length.CompareTo(((byte[])y).Length);
            else
                result = string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);

            if (hash && result == 0)
                result = x.GetHashCode().CompareTo(y.GetHashCode());
            return result;
        }

        //http://osix.net/modules/article/?id=695
        public static void QuickSort2(IList a, int i, int j, IComparer comp)
        {
            int q = Partition2(a, i, j, comp);
            if (q == -1)
                return;
            if (i < q)
                QuickSort2(a, i, q, comp);
            if (q + 1 < j)
                QuickSort2(a, q + 1, j, comp);
        }

        public static int Partition2(IList a, int p, int r, IComparer comp)
        {
            object x = a[(r + p) / 2];
            int i = p - 1;
            int j = r + 1;
            int sc = 0;
            while (true)
            {
                do
                {
                    j--;
                } while (Compare(a[j], x, comp, true) > 0);
                do
                {
                    i++;
                } while (Compare(a[i], x, comp, true) < 0);
                if (i < j)
                {
                    Swap(a, i, j);
                    sc++;
                }
                else
                {
                    // if (j == p && i == p && sc == 0)
                    //     return -1;
                    return j;
                }
            }
        }

        static int Partition1(IList a, int p, int r, IComparer comp, ref int m)
        {
            m = p + ((r - p) / 2);
            int swapc = m;
            for (int i = p; i < m; i++)
                for (int j = r; j > m; j--)
                {
                    int compi = Compare(a[i], a[m], comp, true);
                    int compj = Compare(a[j], a[m], comp, true);
                    if (compi > 0 &&
                        compj < 0)
                    {
                        Swap(a, i, j);
                        swapc = j;
                    }
                    else if (compi > 0)
                    {
                        Swap(a, i, m);
                        swapc = i;
                    }
                    else if (compj < 0)
                    {
                        Swap(a, j, m);
                        swapc = j;
                    }
                }

            return swapc;
        }

    }

    public class QuickSortEventArgs : EventArgs
    {
        private IList list;
        public QuickSortEventArgs(IList list)
        {
            this.list = list;
        }

        public QuickSortEventArgs()
        {
        }

        public IList List
        {
            get { return list; }
            set
            {
                list = value;
            }
        }

        public int Min { get; set; }
        public int Max { get; set; }

        public IComparer Comparer { get; set; }

        public IAsyncResult Result { get; set; }
    }

}

