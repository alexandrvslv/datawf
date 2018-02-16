using System;
using System.Reflection.Emit;
using System.Reflection;

namespace DataWF.Common
{
    public delegate V GetHandler<T, V>(ref T target);
    public delegate void SetHandler<T, V>(ref T target, V value);

    public class RefPropertyInvoker<T, V> : IInvoker// where T : struct
    {
        public RefPropertyInvoker(PropertyInfo info)
        {
            Name = info.Name;
            CanWrite = info.CanWrite;
            DataType = info.PropertyType;
            GetAction = GetInvokerGet(info.GetGetMethod());
            if (CanWrite)
            {
                SetAction = GetInvokerSet(info.GetSetMethod());
            }
        }

        public string Name { get; set; }

        public bool CanWrite { get; private set; }

        public Type DataType { get; set; }

        internal GetHandler<T, V> GetAction { get; private set; }

        internal SetHandler<T, V> SetAction { get; private set; }

        public V Get(ref T target)
        {
            return GetAction(ref target);
        }

        public object Get(object target)
        {
            T item = (T)target;
            return Get(ref item);
        }

        public void Set(ref T target, V value)
        {
            SetAction(ref target, value);
        }

        public void Set(object target, object value)
        {
            T item = (T)target;
            Set(ref item, (V)value);
            target = (object)item;
        }

        private GetHandler<T, V> GetInvokerGet(MethodInfo info)
        {
            var method = new DynamicMethod(EmitInvoker.GetMethodName(info),
                                           typeof(V),
                                           new Type[] { typeof(T).MakeByRefType() },
                                           true);

            ILGenerator il = method.GetILGenerator();

            if (!info.IsStatic)
            {
                il.Emit(OpCodes.Ldarga_S, 0);
                il.Emit(OpCodes.Ldind_Ref);
            }
            il.EmitCall(OpCodes.Call, info, null);
            il.Emit(OpCodes.Ret);
            return (GetHandler<T, V>)method.CreateDelegate(typeof(GetHandler<T, V>));
        }

        private SetHandler<T, V> GetInvokerSet(MethodInfo info)
        {
            DynamicMethod method = new DynamicMethod(EmitInvoker.GetMethodName(info),
                                                     typeof(void),
                                                     new Type[] { typeof(T).MakeByRefType(), typeof(V) },
                                                     true);

            var il = method.GetILGenerator();
            if (!info.IsStatic)
            {
                il.Emit(OpCodes.Ldarga_S, 0);
                il.Emit(OpCodes.Ldind_Ref);
            }
            il.Emit(OpCodes.Ldarg_1);
            il.EmitCall(OpCodes.Call, info, null);
            il.Emit(OpCodes.Ret);
            return (SetHandler<T, V>)method.CreateDelegate(typeof(SetHandler<T, V>));
        }
    }

}
