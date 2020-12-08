using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public static class ListHelperEqualityComparer<T>
    {
        public static readonly IEqualityComparer<T> Default = GetDefaultComparer();

        private static IEqualityComparer<T> GetDefaultComparer()
        {
            var type = typeof(T);
            return (type == typeof(string) ? (IEqualityComparer<T>)StringComparer.Ordinal :
              type == typeof(DateTime) || type == typeof(DateTime?) ? (IEqualityComparer<T>)DateTimePartComparer.Default :
              type == typeof(byte[]) ? (IEqualityComparer<T>)ByteArrayComparer.Default :
              TypeHelper.IsInterface(type, typeof(IEnumerable)) ? (IEqualityComparer<T>)EmitInvoker.CreateObject(typeof(SequenceComparer<>).MakeGenericType(TypeHelper.GetItemType(type))) :
              EqualityComparer<T>.Default);
        }

    }

}

