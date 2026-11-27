using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace DataWF.Invokers
{
    /// <see href="http://www.codeproject.com/KB/cs/FastMethodInvoker.aspx"/>
    /// <see href="http://www.codeproject.com/KB/cs/FastInvokerWrapper.aspx"/>
    public static class EmitHelper
    {
        [RequiresDynamicCode("Generate constructor wrapper method and return its delegare")]
        public static Func<object[], object> WrapConstructor(ConstructorInfo info)
        {
            var dynamicMethod = new DynamicMethod(
                $"Dynamic{info.DeclaringType?.Name.Replace("`1", "")}{"Ctor"}{(info.IsGenericMethod ? "G" : "")}",
                typeof(object),
                new Type[] { typeof(object[]) },
                true);

            var il = dynamicMethod.GetILGenerator();
            var ps = info.GetParameters();
            var locals = new LocalBuilder[ps.Length];

            for (int i = 0; i < ps.Length; i++)
            {
                locals[i] = il.DeclareLocal(ps[i].ParameterType, true);
            }
            for (int i = 0; i < ps.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.EmitFastInt(i);
                il.Emit(OpCodes.Ldelem_Ref);
                il.EmitCastToReference(ps[i].ParameterType);
                il.Emit(OpCodes.Stloc, locals[i]);
            }
            for (int i = 0; i < ps.Length; i++)
            {
                il.Emit(OpCodes.Ldloc, locals[i]);
            }
            il.Emit(OpCodes.Newobj, info);
            il.Emit(OpCodes.Ret);

            return (Func<object[], object>)dynamicMethod.CreateDelegate(typeof(Func<object[], object>));
        }

        [RequiresDynamicCode("Generate field get wrapper method and return its delegare")]
        public static Func<T, V> WrapFieldGet<T, V>(FieldInfo info)
        {
            var dynamicMethod = new DynamicMethod(
                $"Dynamic{info.DeclaringType?.Name}{info.Name}Get",
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
                    il.EmitFastInt((int)val);
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

        [RequiresDynamicCode("Generate field set wrapper method and return its delegare")]
        public static Action<T, V> WrapFieldSet<T, V>(FieldInfo info)
        {
            var dynamicMethod = new DynamicMethod(
                $"Dynamic{info.DeclaringType?.Name}{info.Name}Set",
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

        [RequiresDynamicCode("Generate struct property get wrapper method and return its delegare")]
        public static GetHandler<T, V> WrapRefPropertyGet<T, V>(MethodInfo info)
        {
            var method = new DynamicMethod(EmitHelper.GetMethodName(info),
                                           typeof(V),
                                           new Type[] { typeof(T) },
                                           true);

            ILGenerator il = method.GetILGenerator();

            if (!info.IsStatic)
            {
                il.Emit(OpCodes.Ldarga_S, 0);
                //il.Emit(OpCodes.Ldind_Ref);
            }
            il.EmitCall(OpCodes.Call, info, null);
            il.Emit(OpCodes.Ret);
            return (GetHandler<T, V>)method.CreateDelegate(typeof(GetHandler<T, V>));
        }

        [RequiresDynamicCode("Generate struct property set wrapper method and return its delegare")]
        public static SetHandler<T, V> WrapRefPropertySet<T, V>(MethodInfo info)
        {
            var method = new DynamicMethod(EmitHelper.GetMethodName(info),
                                                     typeof(void),
                                                     new Type[] { typeof(T).MakeByRefType(), typeof(V) },
                                                     true);

            var il = method.GetILGenerator();
            if (!info.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                //il.Emit(OpCodes.Ldind_Ref);
            }
            il.Emit(OpCodes.Ldarg_1);
            il.EmitCall(OpCodes.Call, info, null);
            il.Emit(OpCodes.Ret);
            return (SetHandler<T, V>)method.CreateDelegate(typeof(SetHandler<T, V>));
        }

        [RequiresDynamicCode("Generate property get wrapper method and return its delegare")]
        public static Func<T, V> WrapPropertyGet<T, V>(MethodInfo info)
        {
            var method = new DynamicMethod(info.GetMethodName(),
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

        [RequiresDynamicCode("Generate property set wrapper method and return its delegare")]
        public static Action<T, V> WrapPropertySet<T, V>(MethodInfo info)
        {
            if (info == null)
                return null;
            DynamicMethod method = new DynamicMethod(info.GetMethodName(),
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

        [RequiresDynamicCode("Generate indexed property get wrapper method and return its delegare")]
        public static Func<T, K, V> WrapIndexedGet<T, V, K>(MethodInfo info)
        {
            var ps = info.GetParameters();
            var method = new DynamicMethod(info.GetMethodName(),
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
                    il.EmitFastInt(Convert.ToInt32(ps[i].DefaultValue));
                    //EmitCastToReference(il, ps[i].ParameterType);
                    //il.Emit(OpCodes.Castclass, ps[i].ParameterType);
                }
                else if (ps[i].DefaultValue is int intV)
                    il.EmitFastInt(intV);
                else if (ps[i].DefaultValue is string stringV)
                    il.Emit(OpCodes.Ldstr, stringV);
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

        [RequiresDynamicCode("Generate indexed property set wrapper method and return its delegare")]
        public static Action<T, K, V> WrapIndexedSet<T, K, V>(MethodInfo info)
        {
            var method = new DynamicMethod(info.GetMethodName(),
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

        [RequiresDynamicCode("Generate any method wrapper method and return its delegare")]
        public static Func<T, object[], V> WrapMethod<T, V>(MethodInfo info)
        {
            var method = new DynamicMethod(info.GetMethodName(),
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
                il.EmitFastInt(i);
                il.Emit(OpCodes.Ldelem_Ref);
                il.EmitCastToReference(paramTypes[i]);
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
                    il.EmitFastInt(i);
                    il.Emit(OpCodes.Ldloc, locals[i]);
                    if (locals[i].LocalType.IsValueType)
                        il.Emit(OpCodes.Box, locals[i].LocalType);
                    il.Emit(OpCodes.Stelem_Ref);
                }
            }
            il.Emit(OpCodes.Ret);
            return (Func<T, object[], V>)method.CreateDelegate(typeof(Func<T, object[], V>));
        }

        [RequiresDynamicCode("Generate several call wrapper method and return its delegare")]
        public static Func<T, V> WrapComplexGet<T, V>(string name, List<MemberInvokeInfo> list)
        {
            var first = list.First();
            var method = new DynamicMethod($"{first.Info.DeclaringType?.Name}.{name}Get",
                                           typeof(V),
                                           new Type[] { typeof(T) },
                                           true);

            ILGenerator il = method.GetILGenerator();
            var locals = new LocalBuilder[list.Count];
            var lables = new Label[(list.Count - 1) * 2];
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                locals[i] = il.DeclareLocal(item.Info.GetMemberType());
            }
            for (int i = 0; i < lables.Length; i++)
            {
                var item = list[i / 2];
                if (!item.Info.GetMemberType().IsValueType)
                    lables[i] = il.DefineLabel();
            }

            EmitLoadArgument(il, typeof(T));
            int j = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var type = item.Info.GetMemberType();
                EmitCallGet(il, item.Info);
                if (i < list.Count - 1)
                {
                    il.Emit(OpCodes.Stloc, locals[i]);
                    if (!type.IsValueType)
                    {
                        il.Emit(OpCodes.Ldloc, locals[i]);
                        il.Emit(OpCodes.Brtrue_S, lables[j + 1]);
                        il.MarkLabel(lables[j++]);
                        il.Emit(OpCodes.Ldloc, locals[list.Count - 1]);
                        il.Emit(OpCodes.Ret);
                        il.MarkLabel(lables[j++]);
                    }
                    EmitLoadLocal(il, locals[i]);
                }
            }
            il.Emit(OpCodes.Ret);
            return (Func<T, V>)method.CreateDelegate(typeof(Func<T, V>));
        }

        public static Action<T, V>? WrapComplexSet<T, V>(string name, List<MemberInvokeInfo> list)
        {
            var last = list.Last();
            foreach (var item in list)
            {
                if (item.Info is PropertyInfo propertyInfo
                    && propertyInfo.PropertyType.IsValueType
                    && (!propertyInfo.CanWrite || propertyInfo.GetSetMethod() == null))
                    return null;
            }
            if (last.Info is MethodInfo
                || (last.Info is PropertyInfo lastPropertyInfo
                    && (!lastPropertyInfo.CanWrite || lastPropertyInfo.GetSetMethod() == null)))
            {
                return null;
            }
            var first = list.First();
            var method = new DynamicMethod($"{first.Info.DeclaringType?.Name}.{name}Set",
                                                     typeof(void),
                                                     new Type[] { typeof(T), typeof(V) }, true);

            var il = method.GetILGenerator();
            var locals = new LocalBuilder[list.Count - 1];
            var lables = new Label[list.Count + 1];
            for (int i = 0; i < list.Count - 1; i++)
            {
                var info = list[i];
                locals[i] = il.DeclareLocal(info.Info.GetMemberType());
            }
            for (int i = 0; i <= list.Count; i++)
            {
                lables[i] = il.DefineLabel();
            }
            EmitLoadArgument(il, typeof(T));
            int j = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var type = item.Info.GetMemberType();
                if (i < list.Count - 1)
                {
                    EmitCallGet(il, item.Info);

                    il.Emit(OpCodes.Stloc, locals[i]);
                    if (!type.IsValueType)
                    {
                        il.Emit(OpCodes.Ldloc, locals[i]);
                        il.Emit(OpCodes.Brfalse_S, lables[lables.Length - 1]);
                        il.MarkLabel(lables[j++]);
                    }
                    EmitLoadLocal(il, locals[i]);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_1);
                    EmitCallSet(il, item.Info);
                    for (int r = list.Count - 2; r >= 0; r--)
                    {
                        item = list[r];
                        type = item.Info.GetMemberType();
                        if (type.IsValueType)
                        {
                            if (r == 0)
                                EmitLoadArgument(il, typeof(T));
                            else
                                EmitLoadLocal(il, locals[r - 1]);
                            il.Emit(OpCodes.Ldloc, locals[r]);//EmitLoadLocal(il, locals[r]);
                            EmitCallSet(il, item.Info);
                        }
                    }
                }
            }
            if (j > 0)
            {
                il.MarkLabel(lables[lables.Length - 1]);
            }
            il.Emit(OpCodes.Ret);
            var handler = (Action<T, V>)method.CreateDelegate(typeof(Action<T, V>));
            return handler;
        }

        private static void EmitLoadLocal(ILGenerator il, LocalBuilder local)
        {
            if (local.LocalType.IsValueType && !local.LocalType.IsPrimitive)
                il.Emit(OpCodes.Ldloca_S, local);
            else
                il.Emit(OpCodes.Ldloc, local);
        }

        private static void EmitLoadArgument(ILGenerator il, Type type)
        {
            if (type.IsValueType)
                il.Emit(OpCodes.Ldarga_S, 0);
            else
                il.Emit(OpCodes.Ldarg, 0);
        }

        private static void EmitCallGet(ILGenerator il, MemberInfo info)
        {
            if (info is PropertyInfo pInfo
                && pInfo.GetGetMethod() is MethodInfo mPInfo)
            {
                if (info.DeclaringType?.IsValueType ?? false)
                    il.EmitCall(OpCodes.Call, mPInfo, null);
                else
                    il.EmitCall(OpCodes.Callvirt, mPInfo, null);
            }
            else if (info is MethodInfo mInfo)
            {
                if (info.DeclaringType?.IsValueType ?? false)
                    il.EmitCall(OpCodes.Call, mInfo, null);
                else
                    il.EmitCall(OpCodes.Callvirt, mInfo, null);
            }
            else if (info is FieldInfo fInfo)
            {
                il.Emit(OpCodes.Ldfld, fInfo);
            }
        }

        public static void EmitCallSet(ILGenerator il, MemberInfo info)
        {
            if (info is PropertyInfo pInfo && pInfo.CanWrite
                && pInfo.GetSetMethod() is MethodInfo pMInfo)
            {
                if (info.DeclaringType?.IsValueType ?? false)
                    il.EmitCall(OpCodes.Call, pMInfo, null);
                else
                    il.EmitCall(OpCodes.Callvirt, pMInfo, null);
            }
            else if (info is MethodInfo mInfo)
            {
                if (info.DeclaringType?.IsValueType ?? false)
                    il.EmitCall(OpCodes.Call, mInfo, null);
                else
                    il.EmitCall(OpCodes.Callvirt, mInfo, null);
            }
            else if (info is FieldInfo fInfo)
            {
                if (info.DeclaringType?.IsValueType ?? false)
                    il.Emit(OpCodes.Stfld, fInfo);
                else
                    il.Emit(OpCodes.Stfld, fInfo);
            }
        }

        public static LocalBuilder[] DeclareLocal(this ILGenerator il, ParameterInfo[] ps, int index)
        {
           var locals = new LocalBuilder[ps.Length > index ? ps.Length - (index) : 0];
            for (int i = index; i < ps.Length; i++)
            {
                int j = i - index;
                locals[j] = il.DeclareLocal(ps[i].ParameterType);
                if (ps[i].DefaultValue is Enum)
                    EmitFastInt(il, Convert.ToInt32(ps[i].DefaultValue));
                else if (ps[i].DefaultValue is int intV)
                    EmitFastInt(il, intV);
                else if (ps[i].DefaultValue is string stringV)
                    il.Emit(OpCodes.Ldstr, stringV);
                else if (ps[i].DefaultValue == null)
                    il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Stloc, locals[j]);
            }
            return locals;
        }


        public static void EmitCastToReference(this ILGenerator il, System.Type type)
        {
            if (type.IsValueType)
                il.Emit(OpCodes.Unbox_Any, type);
            else
                il.Emit(OpCodes.Castclass, type);
        }

        public static void EmitBoxIfNeeded(this ILGenerator il, System.Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
        }

        public static void EmitFastInt(this ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    return;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    return;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    return;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    return;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    return;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    return;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    return;
            }

            if (value > -129 && value < 128)
            {
                il.Emit(OpCodes.Ldc_I4_S, (SByte)value);
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4, value);
            }
        }

        public static string GetMethodName(this MethodInfo info)
        {
            return $"Dynamic{info.DeclaringType?.Name}{info.Name}{(info.IsGenericMethod ? "G" : "")}";
        }

    }
}
