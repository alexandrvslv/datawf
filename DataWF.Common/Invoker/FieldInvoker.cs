using System;
using System.Reflection.Emit;
using System.Reflection;

namespace DataWF.Common
{
    public class FieldInvoker<T, V> : Invoker<T, V>
    {
        public FieldInvoker(FieldInfo info)
            : base(info.Name, GetFieldGetInvoker(info), GetFieldSetInvoker(info))
        { }

        public FieldInvoker(string name)
            : this((FieldInfo)TypeHelper.GetMemberInfo(typeof(T), name))
        { }

        public static Func<T, V> GetFieldGetInvoker(FieldInfo info)
        {
            var dynamicMethod = new DynamicMethod(
                string.Format("Dynamic{0}{1}Get", info.DeclaringType.Name, info.Name),
                typeof(V),
                new Type[] { typeof(T) },
                true);

            ILGenerator il = dynamicMethod.GetILGenerator();
            if (!info.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, info);
            }
            else
            {
                var val = info.GetRawConstantValue();
                if (val is int)
                    EmitInvoker.EmitFastInt(il, (int)val);
                else if (val is long)
                    il.Emit(OpCodes.Ldc_I8, (long)val);
                else if (val is float)
                    il.Emit(OpCodes.Ldc_R4, (float)val);
                else
                    il.Emit(OpCodes.Ldsfld, info);
            }
            //EmitBoxIfNeeded(il, info.FieldType);
            il.Emit(OpCodes.Ret);

            return (Func<T, V>)dynamicMethod.CreateDelegate(typeof(Func<T, V>));
        }

        public static Action<T, V> GetFieldSetInvoker(FieldInfo info)
        {
            var dynamicMethod = new DynamicMethod(
                string.Format("Dynamic{0}{1}Set", info.DeclaringType.Name, info.Name),
                typeof(void),
                new Type[] { typeof(T), typeof(V) },
                true);

            ILGenerator il = dynamicMethod.GetILGenerator();
            if (!info.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }
            il.Emit(OpCodes.Ldarg_1);

            if (!info.IsStatic)
                il.Emit(OpCodes.Stfld, info);
            else
                il.Emit(OpCodes.Stsfld, info);
            il.Emit(OpCodes.Ret);

            return (Action<T, V>)dynamicMethod.CreateDelegate(typeof(Action<T, V>));
        }
    }
}
