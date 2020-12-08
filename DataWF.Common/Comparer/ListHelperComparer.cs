using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public static class ListHelperComparer<T>
    {
        public static readonly IComparer<T> Default = GetDefaultComparer();

        private static IComparer<T> GetDefaultComparer()
        {
            var type = typeof(T);
            return (type == typeof(string) ? (IComparer<T>)StringComparer.Ordinal :
              type == typeof(DateTime) || type == typeof(DateTime?) ? (IComparer<T>)DateTimePartComparer.Default :
              type == typeof(byte[]) ? (IComparer<T>)ByteArrayComparer.Default :
              TypeHelper.IsInterface(type, typeof(IEnumerable)) ? (IComparer<T>)EmitInvoker.CreateObject(typeof(SequenceComparer<>).MakeGenericType(TypeHelper.GetItemType(type))) :
              Comparer<T>.Default);
        }
    }

}

