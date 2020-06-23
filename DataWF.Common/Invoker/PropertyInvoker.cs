using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace DataWF.Common
{
    public class PropertyInvoker<T, V> : ActionInvoker<T, V>
    {
        public PropertyInvoker(PropertyInfo info)
            : base(info.Name, GetExpressionGet(info), info.CanWrite ? GetExpressionSet(info) : null)
        { }

        public PropertyInvoker(string name)
            : this((PropertyInfo)TypeHelper.GetMemberInfo(typeof(T), name, out _, false))
        { }

        public static Func<T, V> GetEmitGet(MethodInfo info)
        {
            var method = new DynamicMethod(EmitInvoker.GetMethodName(info),
                typeof(V),
                new Type[] { typeof(T) },
                info.DeclaringType,
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

        public static Action<T, V> GetEmitSet(MethodInfo info)
        {
            if (info == null)
                return null;
            DynamicMethod method = new DynamicMethod(EmitInvoker.GetMethodName(info),
                typeof(void),
                new Type[] { typeof(T), typeof(V) },
                info.DeclaringType,
                true);

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
        //public static Action<T,V>

        //https://www.codeproject.com/Articles/584720/Expression-Based-Property-Getters-and-Setters
        public static Func<T, V> GetExpressionGet(PropertyInfo info)
        {
            var param = Expression.Parameter(typeof(T), "target");
            var property = Expression.Property(param, info);

            return Expression.Lambda<Func<T, V>>(property, param).Compile();
        }

        public static Action<T, V> GetExpressionSet(PropertyInfo info)
        {
            var param = Expression.Parameter(typeof(T), "target");
            var value = Expression.Parameter(typeof(V), "value");
            var proeprty = Expression.Property(param, info);

            return Expression.Lambda<Action<T, V>>(Expression.Assign(proeprty, value), param, value).Compile();
        }
    }

}
