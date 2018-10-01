using DataWF.Common;
using System;
using System.Collections;

namespace DataWF.Data
{
    public static class PullIndexFabric
    {
        private static Type[] ctorTypes = new Type[] { typeof(Pull), typeof(object), typeof(IComparer), typeof(IEqualityComparer) };
        private static Type[] ctor2Types = new Type[] { typeof(Pull), typeof(IComparer), typeof(IEqualityComparer) };

        public static PullIndex Create(Pull pull, Type type, Type keyType, IComparer valueComparer)
        {
            object nullKey = ListIndexFabric.GetNullKey(keyType);
            object keyComparer = null;
            if (type == typeof(string))
            {
                keyComparer = StringComparer.OrdinalIgnoreCase;
            }


            Type gtype = null;
            if (keyType.IsValueType || keyType.IsEnum)
            {
                gtype = typeof(NullablePullIndex<,>).MakeGenericType(type, keyType);
                return (PullIndex)EmitInvoker.CreateObject(gtype, ctor2Types, new object[] { pull, nullKey, valueComparer, keyComparer }, true);
            }
            else
            {
                gtype = typeof(PullIndex<,>).MakeGenericType(type, keyType);
                return (PullIndex)EmitInvoker.CreateObject(gtype, ctorTypes, new object[] { pull, nullKey, valueComparer, keyComparer }, true);
            }

        }
    }



}
