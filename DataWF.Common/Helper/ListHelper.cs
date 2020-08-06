using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DataWF.Common
{

    public static class ListHelper
    {
        public static event Action<QuickSortEventArgs> QuickSortFinished;
        private static readonly AsyncCallback callback = new AsyncCallback(QuickSortFinish);
        private static readonly Action<IList, int, int, IComparer> action = new Action<IList, int, int, IComparer>(QuickSort1);

        public static IEnumerable<object> ToEnumerable(this object obj)
        {
            if (obj is IEnumerable<object> enumerableObject)
                return enumerableObject;
            else if (obj is IEnumerable enumerable)
                return enumerable.Cast<object>();
            else if (obj == null)
                return Enumerable.Empty<object>();
            else
                return Enumerable.Repeat(obj, 1);
        }

        public static IEnumerable<T> ToEnumerable<T>(this object obj)
        {
            if (obj is IEnumerable<T> enumerableObject)
                return enumerableObject;
            else if (obj is IEnumerable enumerable)
                return enumerable.Cast<T>();
            else if (obj == null)
                return Enumerable.Empty<T>();
            else
                return Enumerable.Repeat((T)obj, 1);
        }

        public static bool Contains(IEnumerable enumerable, object value)
        {
            if (enumerable is IList list)
            {
                return list.Contains(value);
            }
            else if (enumerable != null)
            {
                foreach (var item in enumerable)
                {
                    if (item?.Equals(value) ?? false)
                        return true;
                }
            }
            return false;
        }

        public static IEnumerable<object> Intersect(IEnumerable a, IEnumerable b)
        {
            foreach (var item in a)
            {
                if (Contains(b, item))
                    yield return item;
            }
        }

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

        public static NotifyCollectionChangedEventArgs GenerateArgs(NotifyCollectionChangedAction type, object item, int index, int oldIndex, object oldItem)
        {
            NotifyCollectionChangedEventArgs args = null;
            switch (type)
            {
                case NotifyCollectionChangedAction.Reset:
                    args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    if (item is IList list)
                        args = new NotifyCollectionChangedEventArgs(type, list);
                    else
                        args = new NotifyCollectionChangedEventArgs(type, item, index);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index);
                    break;
                case NotifyCollectionChangedAction.Move:
                    args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, index, oldIndex);
                    break;
            }

            return args;
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
            int midpoint;
            int rez;
            while (low <= high)
            {
                midpoint = low + ((high - low) >> 1);

                // check to see if value is equal to item in array
                rez = CompareT(value, array[midpoint], comp);
                if (rez == 0)//check 
                    if (midpoint > 0 && CompareT(value, array[midpoint - 1], comp) <= 0)
                        rez = -1;
                    else if (midpoint < array.Count - 1 && CompareT(value, array[midpoint + 1], comp) >= 0)
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
            int midpoint;
            int rez;
            while (low <= high)
            {
                midpoint = (low + high) >> 1;

                // check to see if value is equal to item in array
                rez = Compare(value, array[midpoint], comp);

                if (rez == 0)//check 
                    if (midpoint > 0 && Compare(value, array[midpoint - 1], comp) <= 0)
                        rez = -1;
                    else if (midpoint < array.Count - 1 && Compare(value, array[midpoint + 1], comp) >= 0)
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
        //public class BitConvert<T> where T : struct
        //{
        //    [StructLayout(LayoutKind.Explicit)]
        //    struct EnumUnion32
        //    {
        //        [FieldOffset(0)]
        //        public T Enum;
        //        [FieldOffset(0)]
        //        public int Int;
        //    }
        //    public static int Enum32ToInt(T e)
        //    {
        //        var u = default(EnumUnion32);
        //        u.Enum = e;
        //        return u.Int;
        //    }
        //    public static T IntToEnum32(int value)
        //    {
        //        var u = default(EnumUnion32);
        //        u.Int = value;
        //        return u.Enum;
        //    }
        //}

        public static bool CheckItemN<T>(T? x, object y, CompareType compare, IComparer<T?> comparer) where T : struct
        {
            bool result = false;
            switch (compare.Type)
            {
                case CompareTypes.Equal:
                    result = EqualN<T>(x, (T?)y);
                    break;
                case CompareTypes.Is:
                    result = x == null;
                    break;
                case CompareTypes.Like:
                    y = (y?.ToString() ?? string.Empty).Trim(new char[] { '%' });
                    result = (x?.ToString() ?? string.Empty).IndexOf((string)y, 0, StringComparison.OrdinalIgnoreCase) >= 0;
                    break;
                case CompareTypes.In:
                    if (x is Enum && y is Enum)
                    {
                        result = ((int)y & (int)(object)x) != 0;//TODO Find the way to aviod BOXING ((int)y & (int)x) != 0;
                    }
                    else if (y is string yString)
                    {
                        result = yString.IndexOf(x?.ToString(), StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    else
                    {
                        foreach (T? item in y.ToEnumerable<T?>())
                        {
                            if (EqualN<T>(x, item))
                            {
                                result = true;
                                break;
                            }
                        }
                    }
                    break;
                case CompareTypes.Contains:
                    result = x.ToEnumerable().Contains(y);
                    break;
                case CompareTypes.Intersect:
                    result = x.ToEnumerable().Intersect(y.ToEnumerable()).Any();
                    break;
                default:
                    int i = Nullable.Compare(x, (T?)y);
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
                    break;
            }

            return compare.Not ? !result : result;
        }

        public static bool CheckItemT<T>(T x, object y, CompareType compare, IComparer<T> comparer)
        {
            bool result = false;
            switch (compare.Type)
            {
                case CompareTypes.Equal:
                    result = EqualT(x, y == null ? default(T) : (T)y);
                    break;
                case CompareTypes.Is:
                    result = x == null;
                    break;
                case CompareTypes.Like:
                    y = (y?.ToString() ?? string.Empty).Trim(new char[] { '%' });
                    result = (x?.ToString() ?? string.Empty).IndexOf((string)y, 0, StringComparison.OrdinalIgnoreCase) >= 0;
                    break;
                case CompareTypes.In:
                    if (x is Enum && y is Enum)
                    {
                        result = ((int)y & (int)(object)x) != 0;//TODO Find the way to aviod BOXING ((int)y & (int)x) != 0;
                    }
                    else if (y is string yString)
                    {
                        result = yString.IndexOf(x?.ToString(), StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    else if (x != null && TypeHelper.IsEnumerable(x.GetType()))
                    {
                        result = x.ToEnumerable<T>().Intersect(y.ToEnumerable<T>()).Any();
                    }
                    else
                    {
                        foreach (T item in y.ToEnumerable<T>())
                        {
                            if (EqualT(x, item))
                            {
                                result = true;
                                break;
                            }
                        }
                    }
                    break;
                case CompareTypes.Contains:
                    result = x.ToEnumerable().Contains(y);
                    break;
                case CompareTypes.Intersect:
                    result = x.ToEnumerable().Intersect(y.ToEnumerable()).Any();
                    break;
                default:
                    int i = CompareT(x, y == null ? default(T) : (T)y, comparer);
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
                    break;
            }

            return compare.Not ? !result : result;
        }

        public static bool CheckItem(object x, object y, CompareType compare, IComparer comparer)
        {
            bool result = false;
            switch (compare.Type)
            {
                case CompareTypes.Equal:
                    result = Equal(x, y);
                    break;
                case CompareTypes.Is:
                    result = x == null || x == DBNull.Value;
                    break;
                case CompareTypes.Like:
                    y = (y?.ToString() ?? string.Empty).Trim(new char[] { '%' });
                    result = (x?.ToString() ?? string.Empty).IndexOf((string)y, 0, StringComparison.OrdinalIgnoreCase) >= 0;
                    break;
                case CompareTypes.In:
                    if (!(x is string) && x is IEnumerable xlist)
                    {
                        x = y;
                        y = xlist;
                    }
                    if (x is Enum && y is Enum)
                    {
                        result = ((int)y & (int)x) != 0;
                    }
                    else if (y is string yString)
                    {
                        result = yString.IndexOf(x?.ToString(), StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    else
                    {
                        foreach (object item in y.ToEnumerable())
                        {
                            if (item is string && !(x is string))
                            {
                                x = x == null ? string.Empty : x.ToString();
                            }

                            if (Equals(item, x))
                            {
                                result = true;
                                break;
                            }
                        }
                    }
                    break;
                case CompareTypes.Contains:
                    result = x.ToEnumerable().Contains(y);
                    break;
                case CompareTypes.Intersect:
                    result = x.ToEnumerable().Intersect(y.ToEnumerable()).Any();
                    break;
                default:
                    int i = Compare(x, y, comparer);
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
                    break;
            }

            return compare.Not ? !result : result;
        }

        public static bool CheckItem(object item, IQuery checkers)
        {
            bool? flag = null;
            var stack = new Stack<CheckStackEntry>(0);
            foreach (var parameter in checkers.Parameters.Where(p => p.IsEnabled))
            {
                bool rez = parameter.Invoker.CheckItem(item, parameter.TypedValue, parameter.Comparer, parameter.Comparision);
                var currParameter = parameter;
                if ((currParameter.Group & QueryGroup.Begin) == QueryGroup.Begin)
                {
                    stack.Push(new CheckStackEntry { Flag = rez, Parameter = currParameter });
                    continue;
                }
                else if (stack.Count > 0)
                {
                    var entry = stack.Pop();
                    entry.Flag = Concat(entry.Flag, rez, currParameter);

                    if ((currParameter.Group & QueryGroup.End) == QueryGroup.End)
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

        public static IEnumerable Search(IEnumerable items, IInvoker invoker, object typedValue, CompareType comparer, IComparer comparision)
        {
            foreach (var item in items)
            {
                if (item != null && invoker.CheckItem(item, typedValue, comparer, comparision))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable Search(IEnumerable items, IQueryParameter param)
        {
            return Search(items, param.Invoker, param.TypedValue, param.Comparer, param.Comparision);
        }

        public static IEnumerable<T> Search<T>(IEnumerable<T> items, QueryParameter<T> param)
        {
            foreach (var item in items)
            {
                if (item != null && param.Invoker.CheckItem(item, param.TypedValue, param.Comparer, param.Comparision))
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
                if ((curParameter.Group & QueryGroup.Begin) == QueryGroup.Begin)
                {
                    stack.Push(new SelectStackEntry<T>() { Buffer = temp, Parameter = curParameter });
                    continue;
                }
                else if (stack.Count > 0)
                {
                    var entry = stack.Pop();
                    entry.Buffer = Concat(entry.Buffer, temp, curParameter);
                    if ((curParameter.Group & QueryGroup.End) == QueryGroup.End)
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
            if (param.Comparer.Type == CompareTypes.Distinct)
            {
                return Distinct(items, param.Invoker);
            }

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
            if (param.Comparer.Type == CompareTypes.Distinct)
            {
                return Distinct(items, param.Invoker);
            }
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

        public static IEnumerable<T> Distinct<T>(IEnumerable<T> items, IInvoker param, IComparer comparer = null)
        {
            var oldValue = (object)null;
            var list = items.ToList();
            ListHelper.QuickSort(list, comparer ?? ((IInvokerExtension)param).CreateComparer<T>(ListSortDirection.Descending));
            foreach (var item in list)
            {
                var newValue = param.GetValue(item);
                if (!Equal(newValue, oldValue))
                {
                    oldValue = newValue;
                    if (newValue != null)
                    {
                        yield return item;
                    }
                }
            }
        }

        public static IEnumerable Distinct(IEnumerable items, IInvoker param, IComparer comparer = null)
        {
            var oldValue = (object)null;
            var list = items.Cast<object>().ToList();
            ListHelper.QuickSort(list, comparer ?? ((IInvokerExtension)param).CreateComparer(param.TargetType, ListSortDirection.Descending));
            foreach (var item in list)
            {
                var newValue = param.GetValue(item);
                if (!Equal(newValue, oldValue))
                {
                    oldValue = newValue;
                    if (newValue != null)
                    {
                        yield return item;
                    }
                }
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
            //Debug.WriteLine("QuickSort");
            //intcomp c = new intcomp();

            //ArrayList a = GetArray();
            //long time = Environment.TickCount;
            //a.Sort(c);
            //time = Environment.TickCount - time;
            //Debug.WriteLine("Time Elapsed standart: " + time + " msecs.");
            //time = Environment.TickCount;
            //a.Sort(c);
            //time = Environment.TickCount - time;
            //Debug.WriteLine("S Time Elapsed standart: " + time + " msecs.");

            //a = GetArray();
            //time = Environment.TickCount;
            //QuickSort2(a, 0, a.Count - 1, c);
            //time = Environment.TickCount - time;
            //Debug.WriteLine("Time Elapsed osix: " + time + " msecs.");

            //time = Environment.TickCount;
            //QuickSort2(a, 0, a.Count - 1, c);
            //time = Environment.TickCount - time;
            //Debug.WriteLine("S Time Elapsed osix: " + time + " msecs.");

            //a = GetArray();
            //time = Environment.TickCount;
            //QuickSort1(a, 0, a.Count - 1, c);
            //time = Environment.TickCount - time;
            //Debug.WriteLine("Time Elapsed this: " + time + " msecs.");

            //time = Environment.TickCount;
            //QuickSort1(a, 0, a.Count - 1, c);
            //time = Environment.TickCount - time;
            //Debug.WriteLine("S Time Elapsed this: " + time + " msecs.");
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
                while (CompareT<T>(a[i], x, comp) < 0) ++i;
                while (CompareT<T>(a[j], x, comp) > 0) --j;
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
                while (Compare(a[i], x, comp) < 0) ++i;
                while (Compare(a[j], x, comp) > 0) --j;
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
            n += 1;
            for (i = s + 1; i < n; i++)
            {
                T t = x[i];
                for (j = i; j > s && CompareT(x[j - 1], t, comp) > 0; j--)
                    x[j] = x[j - 1];

                x[j] = t;
            }
        }

        public static void LinearSort(IList x, int s, int n, IComparer comp)
        {
            int i, j;
            n += 1;
            for (i = s + 1; i < n; i++)
            {
                object t = x[i];
                for (j = i; j > s && Compare(x[j - 1], t, comp) > 0; j--)
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

        public static bool EqualT<T>(T x, T y)
        {
            // var type = TypeHelper.CheckNullable(typeof(T));
            if (x is string)
            {
                return string.Equals(x?.ToString(), y?.ToString(), StringComparison.OrdinalIgnoreCase);
            }
            if (x is byte[] xByte && y is byte[] yByte)
            {
                return ByteArrayComparer.Default.Equals(xByte, yByte);
            }
            if (x is DateTime xDate && y is DateTime yDate)
            {
                return DateTimePartComparer.Default.Equals(xDate, yDate);
            }
            //if (x is IEnumerable<object> xEnumerable && y is IEnumerable<object> yEnumerable)
            //{
            //    return xEnumerable.SequenceEqual(yEnumerable);
            //}
            return EqualityComparer<T>.Default.Equals(x, y);
        }

        public static bool EqualN<T>(T? x, T? y) where T : struct
        {
            if (x is DateTime xDate && y is DateTime yDate)
            {
                return DateTimePartComparer.Default.Equals(xDate, yDate);
            }
            return Nullable.Equals<T>(x, y);
        }

        public static bool Equal(object x, object y)
        {
            if (x == null)
            {
                return y == null;
            }
            else if (y == null)
            {
                return false;
            }
            bool result = false;

            if (x is Enum || y is Enum)
            {
                result = ((int)x).Equals((int)y);
            }
            else if (x is string || y is string)
            {
                result = string.Equals(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
            }
            else if (x is DateTime xDate && y is DateTime yDate)
            {
                return DateTimePartComparer.Default.Equals(xDate, yDate);
            }
            else if (x is byte[] xByte && y is byte[] yByte)
            {
                result = Helper.CompareByteAsSpan(xByte, yByte);
            }
            //else if (x is IEnumerable<object> xEnumerable && y is IEnumerable<object> yEnumerable)
            //{
            //    result = xEnumerable.SequenceEqual(yEnumerable);
            //}
            else if (x.Equals(y))
            {
                result = true;
            }
            return result;
        }

        public static int CompareT<T>(T x, T y, IComparer<T> comp)
        {
            int result;
            bool hash = false;
            if (comp != null)
            {
                result = comp.Compare(x, y);
                hash = x != null && y != null && result == 0 && !EqualT(x, y);
            }
            else if (x == null)
            {
                result = y == null ? 0 : -1;
            }
            else if (y == null)
            {
                result = 1;
            }
            else if (x is string xs && y is string ys)
            {
                result = string.Compare(xs, ys, StringComparison.OrdinalIgnoreCase);
            }
            else if (x is DateTime xd && y is DateTime yd)
            {
                result = DateTimePartComparer.Default.Compare(xd, yd);
            }
            else if (x is byte[] xb && y is byte[] yb)
            {
                result = xb.Length.CompareTo(yb.Length);
            }
            else if (x is IComparable<T> compareable)
            {
                result = compareable.CompareTo(y);
            }
            else if (x is IComparable xc)
            {
                result = xc.CompareTo(y);
            }
            else if (EqualT(x, y))
            {
                result = 0;
            }
            else if (x is IEnumerable xEnumerable && y is IEnumerable yEnumerable)
            {
                result = xEnumerable.SequenceCompare(yEnumerable);
            }
            else
            {
                result = string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
                hash = true;
            }

            if (hash && result == 0)
                result = x.GetHashCode().CompareTo(y.GetHashCode());
            return result;
        }

        public static int Compare(object x, object y, IComparer comp)
        {
            int result;
            bool hash = false;
            if (comp != null)
            {
                result = comp.Compare(x, y);
                hash = x != null && y != null && result == 0 && !Equal(x, y);
            }
            else if (x == null)
            {
                result = (y == null) ? 0 : -1;
            }
            else if (y == null)
            {
                result = 1;
            }
            else if (x is string xs && y is string ys)
            {
                result = string.Compare(xs, ys, StringComparison.OrdinalIgnoreCase);
            }
            else if (x is DateTime xd && y is DateTime yd)
            {
                result = DateTimePartComparer.Default.Compare(xd, yd);
            }
            else if (x is byte[] xb && y is byte[] yb)
            {
                result = xb.Length.CompareTo(yb.Length);
            }
            else if (x is IComparable xc)//x.GetType() == y.GetType() &&
            {
                result = xc.CompareTo(y);
            }
            else if (Equal(x, y))
            {
                result = 0;
            }
            else if (x is IEnumerable xEnumerable && y is IEnumerable yEnumerable)
            {
                result = xEnumerable.SequenceCompare(yEnumerable);
            }
            else
            {
                result = string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
                hash = true;
            }

            if (hash && result == 0)
                result = x.GetHashCode().CompareTo(y.GetHashCode());
            return result;
        }

        public static int SequenceCompare(this IEnumerable xEnumerable, IEnumerable yEnumerable)
        {
            int result = 0;
            var xEnumer = xEnumerable.GetEnumerator();
            var yEnumer = yEnumerable.GetEnumerator();
            bool xExist = true;
            bool yExist = true;
            while (true)
            {
                xExist = xEnumer.MoveNext();
                yExist = yEnumer.MoveNext();
                if (xExist && !yExist)
                {
                    result = 1;
                    break;
                }
                else if (!xExist && yExist)
                {
                    result = -1;
                    break;
                }
                else if (xExist && yExist)
                {
                    result = Compare(xEnumer.Current, yEnumer.Current, null);
                    if (result != 0)
                        break;
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        public static int SequenceCompare<T>(this IEnumerable<T> xEnumerable, IEnumerable<T> yEnumerable)
        {
            int result = 0;
            var xEnumer = xEnumerable.GetEnumerator();
            var yEnumer = yEnumerable.GetEnumerator();
            bool xExist = true;
            bool yExist = true;
            while (true)
            {
                xExist = xEnumer.MoveNext();
                yExist = yEnumer.MoveNext();
                if (xExist && !yExist)
                {
                    result = 1;
                    break;
                }
                else if (!xExist && yExist)
                {
                    result = -1;
                    break;
                }
                else if (xExist && yExist)
                {
                    result = CompareT(xEnumer.Current, yEnumer.Current, null);
                    if (result != 0)
                        break;
                }
                else
                {
                    break;
                }
            }

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
                } while (Compare(a[j], x, comp) > 0);
                do
                {
                    i++;
                } while (Compare(a[i], x, comp) < 0);
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
                    int compi = Compare(a[i], a[m], comp);
                    int compj = Compare(a[j], a[m], comp);
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

