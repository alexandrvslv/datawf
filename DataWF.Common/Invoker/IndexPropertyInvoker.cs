using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq.Expressions;

namespace DataWF.Common
{
    public class IndexPropertyInvoker<T, V, K> : ActionIndexInvoker<T, V, K>
    {
        public static IndexPropertyInvoker<T, V, K> Create(string name)
        {
            var property = (PropertyInfo)TypeHelper.GetMemberInfo(typeof(T), name, out var index, false);
            return new IndexPropertyInvoker<T, V, K>(property, index);
        }

        public IndexPropertyInvoker(PropertyInfo info, object index)
            : this(info, (K)index)
        {
        }

        public IndexPropertyInvoker(PropertyInfo info, K index)
            : base(info.Name, GetExpressionGet(info), info.CanWrite ? GetExpressionSet(info) : null)
        {
            Index = index;
        }

        public static Func<T, K, V> GetInvokerGet(MethodInfo info)
        {
            var ps = info.GetParameters();
            var method = new DynamicMethod(EmitInvoker.GetMethodName(info),
                typeof(V),
                new Type[] { typeof(T), typeof(K) },
                true);

            ILGenerator il = method.GetILGenerator();

            if (!info.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }
            if (ps.Length > 0)
            {
                il.Emit(OpCodes.Ldarg_1);
            }
            for (int i = 1; i < ps.Length; i++)
            {
                //if (ps[i].ParameterType.IsByRef)
                //    il.Emit(OpCodes.Ldloca_S, locals[i - 1]);
                //else
                //    il.Emit(OpCodes.Ldloc, locals[i - 1]);
                if (ps[i].DefaultValue is Enum)
                {
                    EmitInvoker.EmitFastInt(il, (int)ps[i].DefaultValue);
                    //EmitCastToReference(il, ps[i].ParameterType);
                    //il.Emit(OpCodes.Castclass, ps[i].ParameterType);
                }
                else if (ps[i].DefaultValue is int)
                    EmitInvoker.EmitFastInt(il, (int)ps[i].DefaultValue);
                else if (ps[i].DefaultValue is string)
                    il.Emit(OpCodes.Ldstr, (string)ps[i].DefaultValue);
                else if (ps[i].DefaultValue == null)
                    il.Emit(OpCodes.Ldnull);
            }
            if (info.IsStatic)
                il.EmitCall(OpCodes.Call, info, null);
            else
                il.EmitCall(OpCodes.Callvirt, info, null);

            il.Emit(OpCodes.Ret);
            return (Func<T, K, V>)method.CreateDelegate(typeof(Func<T, K, V>));
        }

        public static Action<T, K, V> GetInvokerSet(MethodInfo info)
        {
            DynamicMethod method = new DynamicMethod(EmitInvoker.GetMethodName(info),
                                                     typeof(void),
                                                     new Type[] { typeof(T), typeof(K), typeof(V) }, true);

            var ps = info.GetParameters();
            var il = method.GetILGenerator();

            if (!info.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }
            if (ps.Length > 0)
            {
                il.Emit(OpCodes.Ldarg_1);
            }
            if (ps.Length > 1)
            {
                il.Emit(OpCodes.Ldarg_2);
            }
            for (int i = 2; i < ps.Length; i++)
            {
                //if (ps[i].ParameterType.IsByRef)
                //    il.Emit(OpCodes.Ldloca_S, locals[i - 2]);
                //else
                //    il.Emit(OpCodes.Ldloc, locals[i - 2]);
            }
            if (info.IsStatic)
                il.EmitCall(OpCodes.Call, info, null);
            else
                il.EmitCall(OpCodes.Callvirt, info, null);

            il.Emit(OpCodes.Ret);
            return (Action<T, K, V>)method.CreateDelegate(typeof(Action<T, K, V>));
        }

        public static Func<T, K, V> GetExpressionGet(PropertyInfo info)
        {
            if (info.GetMethod == null)
                return null;

            var index = Expression.Parameter(typeof(K), "index");
            var target = Expression.Parameter(typeof(T), "target");
            var property = Expression.Property(info.GetMethod.IsStatic ? null : target, info, index);

            return Expression.Lambda<Func<T, K, V>>(property, target, index).Compile();
        }

        public static Action<T, K, V> GetExpressionSet(PropertyInfo info)
        {
            if (info.SetMethod == null)
                return null;

            var index = Expression.Parameter(typeof(K), "index");
            var target = Expression.Parameter(typeof(T), "target");
            var value = Expression.Parameter(typeof(V), "value");
            var property = Expression.Property(info.SetMethod.IsStatic ? null : target, info, index);

            return Expression.Lambda<Action<T, K, V>>(Expression.Assign(property, value), target, index, value).Compile();
        }
    }

}
