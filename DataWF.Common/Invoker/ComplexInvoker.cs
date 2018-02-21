using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace DataWF.Common
{
    public class ComplexInvoker<T, V> : Invoker<T, V>
    {
        public ComplexInvoker(string property, List<MemberInfo> list)
            : base(property, GetInvokerGet(property, list), GetInvokerSet(property, list))
        {

        }

        public ComplexInvoker(string property)
            : this(property, TypeHelper.GetMemberInfoList(typeof(T), property))
        {
        }

        public static Func<T, V> GetInvokerGet(string name, List<MemberInfo> list)
        {
            var first = list.First();
            var method = new DynamicMethod($"{first.DeclaringType.Name}.{name}Get",
                                           typeof(V),
                                           new Type[] { typeof(T) },
                                           true);

            ILGenerator il = method.GetILGenerator();
            var locals = new LocalBuilder[list.Count];
            var lables = new Label[(list.Count - 1) * 2];
            for (int i = 0; i < list.Count; i++)
            {
                var info = list[i];
                locals[i] = il.DeclareLocal(TypeHelper.GetMemberType(info));
            }
            for (int i = 0; i < lables.Length; i++)
            {
                var info = list[i / 2];
                if (!TypeHelper.GetMemberType(info).IsValueType)
                    lables[i] = il.DefineLabel();
            }

            EmitLoadArgument(il, typeof(T));
            int j = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var info = list[i];
                var type = TypeHelper.GetMemberType(info);
                EmitCallGet(il, info);
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

        public static Action<T, V> GetInvokerSet(string name, List<MemberInfo> list)
        {
            var last = list.Last();
            foreach (var item in list)
            {
                 if(item is PropertyInfo 
                 && ((PropertyInfo)item).PropertyType.IsValueType 
                 && (!((PropertyInfo)item).CanWrite ||  ((PropertyInfo)last).GetSetMethod() == null))
                    return null;
            }
            if (last is MethodInfo 
            ||  (last is PropertyInfo 
            && (!((PropertyInfo)last).CanWrite || ((PropertyInfo)last).GetSetMethod() == null)))
            {
                return null;
            }
            var first = list.First();
            DynamicMethod method = new DynamicMethod($"{first.DeclaringType.Name}.{name}Set",
                                                     typeof(void),
                                                     new Type[] { typeof(T), typeof(V) }, true);

            var il = method.GetILGenerator();
            var locals = new LocalBuilder[list.Count - 1];
            var lables = new Label[list.Count + 1];
            for (int i = 0; i < list.Count - 1; i++)
            {
                var info = list[i];
                locals[i] = il.DeclareLocal(TypeHelper.GetMemberType(info));
            }
            for (int i = 0; i <= list.Count; i++)
            {
                lables[i] = il.DefineLabel();
            }
            EmitLoadArgument(il, typeof(T));
            int j = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var info = list[i];
                var type = TypeHelper.GetMemberType(info);
                if (i < list.Count - 1)
                {
                    EmitCallGet(il, info);

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
                    EmitCallSet(il, info);
                    for (int r = list.Count - 2; r >= 0; r--)
                    {
                        info = list[r];
                        type = TypeHelper.GetMemberType(info);
                        if (type.IsValueType)
                        {
                            if (r == 0)
                                EmitLoadArgument(il, typeof(T));
                            else
                                EmitLoadLocal(il, locals[r - 1]);
                            il.Emit(OpCodes.Ldloc, locals[r]);//EmitLoadLocal(il, locals[r]);
                            EmitCallSet(il, info);
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
            if (info is PropertyInfo)
            {
                if (info.DeclaringType.IsValueType)
                    il.EmitCall(OpCodes.Call, ((PropertyInfo)info).GetGetMethod(), null);
                else
                    il.EmitCall(OpCodes.Callvirt, ((PropertyInfo)info).GetGetMethod(), null);
            }
            else if (info is MethodInfo)
            {
                if (info.DeclaringType.IsValueType)
                    il.EmitCall(OpCodes.Call, (MethodInfo)info, null);
                else
                    il.EmitCall(OpCodes.Callvirt, (MethodInfo)info, null);
            }
            else if (info is FieldInfo)
            {
                il.Emit(OpCodes.Ldfld, (FieldInfo)info);
            }
        }

        public static void EmitCallSet(ILGenerator il, MemberInfo info)
        {
            if (info is PropertyInfo && ((PropertyInfo)info).CanWrite)
            {
                if (info.DeclaringType.IsValueType)
                    il.EmitCall(OpCodes.Call, ((PropertyInfo)info).GetSetMethod(), null);
                else
                    il.EmitCall(OpCodes.Callvirt, ((PropertyInfo)info).GetSetMethod(), null);
            }
            else if (info is MethodInfo)
            {
                if (info.DeclaringType.IsValueType)
                    il.EmitCall(OpCodes.Call, (MethodInfo)info, null);
                else
                    il.EmitCall(OpCodes.Callvirt, (MethodInfo)info, null);
            }
            else if (info is FieldInfo)
            {
                if (info.DeclaringType.IsValueType)
                    il.Emit(OpCodes.Stfld, (FieldInfo)info);
                else
                    il.Emit(OpCodes.Stfld, (FieldInfo)info);
            }
        }


    }

}
