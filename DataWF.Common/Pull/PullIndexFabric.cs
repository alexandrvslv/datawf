using DataWF.Common;
using System;
using System.Collections;

namespace DataWF.Common
{
    public static class PullIndexFabric
    {
        private static readonly Type[] ctorTypes = new Type[] { typeof(Pull), typeof(object), typeof(IComparer), typeof(IEqualityComparer) };

        public static PullIndex Create(Pull pull, Type type, Type keyType, IComparer valueComparer)
        {
            object nullKey = ListIndexFabric.GetNullKey(keyType);
            object keyComparer = null;
            if (keyType == typeof(string))
            {
                keyComparer = StringComparer.OrdinalIgnoreCase;
            }

            var gtype = typeof(PullIndex<,>).MakeGenericType(type, keyType);
            return (PullIndex)EmitInvoker.CreateObject(gtype, ctorTypes, new object[] { pull, nullKey, valueComparer, keyComparer }, true);

        }
    }



}
