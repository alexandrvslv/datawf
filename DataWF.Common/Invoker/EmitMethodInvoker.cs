using System;
using System.Reflection.Emit;
using System.Reflection;

namespace DataWF.Common
{
    public class EmitMethodInvoker<T, V> : IndexInvoker<T, V, object[]>
    {

        public EmitMethodInvoker(MethodInfo infoGet, MethodInfo infoSet)
            : base(infoGet.Name, GetMethodInvoker(infoGet))
        {

        }

        public V Invoke(T target, params object[] parameters)
        {
            return GetAction(target, parameters);
        }

        public object Invoke(object target, params object[] parameters)
        {
            return Invoke((T)target, parameters);
        }

        public static Func<T, object[], V> GetMethodInvoker(MethodInfo info)
        {
            var method = new DynamicMethod(EmitInvoker.GetMethodName(info),
                typeof(V),
                new Type[] { typeof(T), typeof(object[]) },
                true);

            var il = method.GetILGenerator();
            var ps = info.GetParameters();
            var paramTypes = new Type[ps.Length];
            var locals = new LocalBuilder[paramTypes.Length];

            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                    paramTypes[i] = ps[i].ParameterType.GetElementType();
                else
                    paramTypes[i] = ps[i].ParameterType;
            }
            for (int i = 0; i < paramTypes.Length; i++)
            {
                locals[i] = il.DeclareLocal(paramTypes[i], true);
            }
            for (int i = 0; i < paramTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_1);
                EmitInvoker.EmitFastInt(il, i);
                il.Emit(OpCodes.Ldelem_Ref);
                EmitInvoker.EmitCastToReference(il, paramTypes[i]);
                il.Emit(OpCodes.Stloc, locals[i]);
            }
            if (!info.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }
            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                    il.Emit(OpCodes.Ldloca_S, locals[i]);
                else
                    il.Emit(OpCodes.Ldloc, locals[i]);
            }
            if (info.IsStatic)
                il.EmitCall(OpCodes.Call, info, null);
            else
                il.EmitCall(OpCodes.Callvirt, info, null);

            if (info.ReturnType == typeof(void))
                il.Emit(OpCodes.Ldnull);

            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    EmitInvoker.EmitFastInt(il, i);
                    il.Emit(OpCodes.Ldloc, locals[i]);
                    if (locals[i].LocalType.IsValueType)
                        il.Emit(OpCodes.Box, locals[i].LocalType);
                    il.Emit(OpCodes.Stelem_Ref);
                }
            }
            il.Emit(OpCodes.Ret);
            return (Func<T, object[], V>)method.CreateDelegate(typeof(Func<T, object[], V>));
        }
    }

}
