using System;
using System.Reflection.Emit;
using System.Reflection;

namespace DataWF.Common
{
    public class PropertyInvoker<T, V> : Invoker<T, V>
    {
        public PropertyInvoker(PropertyInfo info)
            : base(info.Name, GetInvokerGet(info.GetGetMethod()), info.CanWrite ? GetInvokerSet(info.GetSetMethod()) : null)
        { }

        public PropertyInvoker(string name)
            : this((PropertyInfo)TypeHelper.GetMemberInfo(typeof(T), name))
        { }

        public static Func<T, V> GetInvokerGet(MethodInfo info)
        {
            var method = new DynamicMethod(EmitInvoker.GetMethodName(info),
                typeof(V),
                new Type[] { typeof(T) },
                true);

            ILGenerator il = method.GetILGenerator();

            if (!info.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }
            if (info.IsStatic)
            {
                il.EmitCall(OpCodes.Call, info, null);
            }
            else
            {
                il.EmitCall(OpCodes.Callvirt, info, null);
            }

            il.Emit(OpCodes.Ret);
            return (Func<T, V>)method.CreateDelegate(typeof(Func<T, V>));
        }

        public static Action<T, V> GetInvokerSet(MethodInfo info)
        {
            if (info == null)
                return null; 
            DynamicMethod method = new DynamicMethod(EmitInvoker.GetMethodName(info),
                typeof(void),
                new Type[] { typeof(T), typeof(V) }, true);

            var il = method.GetILGenerator();
            if (!info.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }

            il.Emit(OpCodes.Ldarg_1);

            if (info.IsStatic)
            {
                il.EmitCall(OpCodes.Call, info, null);
            }
            else
            {
                il.EmitCall(OpCodes.Callvirt, info, null);
            }
            il.Emit(OpCodes.Ret);
            return (Action<T, V>)method.CreateDelegate(typeof(Action<T, V>));
        }
    }

}
