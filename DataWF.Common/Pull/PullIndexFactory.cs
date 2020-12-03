using DataWF.Common;
using System;
using System.Collections;

namespace DataWF.Common
{
    public static class PullIndexFactory
    {
        private static readonly Type[] ctorTypes = new Type[] { typeof(Pull), typeof(object), typeof(IComparer), typeof(IEqualityComparer) };

        public static PullIndex Create(Pull pull, Type type, Type keyType, IComparer valueComparer)
        {
            object keyComparer = null;
            if (keyType == typeof(string))
                keyComparer = StringComparer.OrdinalIgnoreCase;
            else if (keyType == typeof(byte[]))
                keyComparer = ByteArrayComparer.Default;

            object nullKey = ListIndexFactory.GetNullKey(keyType);
            return (PullIndex)EmitInvoker.CreateObject(typeof(PullIndex<,>).MakeGenericType(type, keyType),
                ctorTypes,
                new object[] { pull, nullKey, valueComparer, keyComparer }, true);
        }
    }

}
