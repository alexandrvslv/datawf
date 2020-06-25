using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq.Expressions;

namespace DataWF.Common
{
    public class ComplexInvoker<T, V> : ActionInvoker<T, V>
    {
        public ComplexInvoker(string property, List<MemberParseInfo> list)
            : base(property, GetExpressionGet(property, list), GetExpressionSet(property, list))
        {

        }

        public ComplexInvoker(string property)
            : this(property, TypeHelper.GetMemberInfoList(typeof(T), property))
        {
        }

        public static Func<T, V> GetExpressionGet(string name, List<MemberParseInfo> list)
        {
            var type = typeof(T);
            var body = (Expression)null;
            var target = Expression.Parameter(typeof(T), "target");
            for (int i = 0; i < list.Count; i++)
            {
                var info = list[i];
                var param = (Expression)null;
                if (i == 0)
                {
                    param = target;
                }
                else
                {
                    param = body;
                }
                Expression call = Call(param, info, i);
                body = CreateNullPropagationExpression(body ?? param, call, info.Info);
            }
            return Expression.Lambda<Func<T, V>>(body, target).Compile();
        }

        private static Expression Call(Expression param, MemberParseInfo info, int i)
        {
            var call = (Expression)null;
            if (info.Info is PropertyInfo propertyInfo)
            {
                if (info.Index != null)
                {
                    var index = Expression.Constant(info.Index, info.Index.GetType());
                    call = Expression.Property(param, propertyInfo, index);
                }
                else
                {
                    call = Expression.Property(param, propertyInfo);
                }
            }
            else if (info.Info is FieldInfo fieldInfo)
            {
                call = Expression.Field(param, fieldInfo);
            }
            else if (info.Info is MethodInfo methodInfo)
            {
                call = Expression.Call(param, methodInfo);
            }

            return call;
        }

        //https://stackoverflow.com/a/39617419/4682355
        public static Expression CreateNullPropagationExpression(Expression o, Expression call, MemberInfo memberInfo)
        {
            Expression propertyAccess = call;

            var propertyType = TypeHelper.GetMemberType(memberInfo);

            if (propertyType.IsValueType && !TypeHelper.IsNullable(propertyType))
                return propertyAccess;

            var nullResult = Expression.Default(propertyAccess.Type);

            var condition = Expression.Equal(o, Expression.Constant(null, o.Type));

            return Expression.Condition(condition, nullResult, propertyAccess);
        }

        public static Action<T, V> GetExpressionSet(string name, List<MemberParseInfo> list)
        {
            var last = list.Last();
            var first = list.First();
            var target = Expression.Parameter(typeof(T), "target");
            var value = Expression.Parameter(typeof(V), "value");
            var returnLabel = Expression.Label();
            var locals = new ParameterExpression[list.Count - 1];
            var labels = new LabelTarget[list.Count];
            List<Expression> body = new List<Expression>();
            for (int i = 0; i < list.Count - 1; i++)
            {
                var info = list[i];
                locals[i] = Expression.Variable(TypeHelper.GetMemberType(info.Info), info.Info.Name.ToLower());
            }

            for (int i = 0; i < list.Count; i++)
            {
                labels[i] = Expression.Label();
            }
            int j = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var info = list[i];
                var type = TypeHelper.GetMemberType(info.Info);
                if (i < list.Count - 1)
                {
                    var call = Call(i == 0 ? target : locals[i - 1], info, i);
                    body.Add(Expression.Assign(locals[i], call));

                    if (!type.IsValueType || TypeHelper.IsNullable(type))
                    {
                        body.Add(Expression.IfThenElse(Expression.Equal(locals[i], Expression.Constant(null, type)),
                            Expression.Return(returnLabel),
                            Expression.Label(labels[j++])));
                    }
                }
                else
                {
                    for (int r = list.Count - 1; r >= 0; r--)
                    {
                        info = list[r];
                        type = TypeHelper.GetMemberType(info.Info);
                        if (type.IsValueType || r == list.Count - 1)
                        {
                            var param = r == 0 ? target : locals[r - 1];
                            var setValue = r == list.Count - 1 ? value : locals[r];
                            var call = Call(param, info, r);
                            body.Add(Expression.Assign(call, setValue));
                        }
                    }
                }
            }
            if (j > 0)
            {
                body.Add(Expression.Label(returnLabel));
            }

            return Expression.Lambda<Action<T, V>>(Expression.Block(locals, body), target, value).Compile();
        }

        public static Func<T, V> GetInvokerGet(string name, List<MemberParseInfo> list)
        {
            var first = list.First();
            var method = new DynamicMethod($"{first.Info.DeclaringType.Name}.{name}Get",
                                           typeof(V),
                                           new Type[] { typeof(T) },
                                           true);

            ILGenerator il = method.GetILGenerator();
            var locals = new LocalBuilder[list.Count];
            var lables = new Label[(list.Count - 1) * 2];
            for (int i = 0; i < list.Count; i++)
            {
                var info = list[i];
                locals[i] = il.DeclareLocal(TypeHelper.GetMemberType(info.Info));
            }
            for (int i = 0; i < lables.Length; i++)
            {
                var info = list[i / 2];
                if (!TypeHelper.GetMemberType(info.Info).IsValueType)
                    lables[i] = il.DefineLabel();
            }

            EmitLoadArgument(il, typeof(T));
            int j = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var info = list[i];
                var type = TypeHelper.GetMemberType(info.Info);
                EmitCallGet(il, info.Info);
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

        public static Action<T, V> GetInvokerSet(string name, List<MemberParseInfo> list)
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
            DynamicMethod method = new DynamicMethod($"{first.Info.DeclaringType.Name}.{name}Set",
                                                     typeof(void),
                                                     new Type[] { typeof(T), typeof(V) }, true);

            var il = method.GetILGenerator();
            var locals = new LocalBuilder[list.Count - 1];
            var lables = new Label[list.Count + 1];
            for (int i = 0; i < list.Count - 1; i++)
            {
                var info = list[i];
                locals[i] = il.DeclareLocal(TypeHelper.GetMemberType(info.Info));
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
                var type = TypeHelper.GetMemberType(info.Info);
                if (i < list.Count - 1)
                {
                    EmitCallGet(il, info.Info);

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
                    EmitCallSet(il, info.Info);
                    for (int r = list.Count - 2; r >= 0; r--)
                    {
                        info = list[r];
                        type = TypeHelper.GetMemberType(info.Info);
                        if (type.IsValueType)
                        {
                            if (r == 0)
                                EmitLoadArgument(il, typeof(T));
                            else
                                EmitLoadLocal(il, locals[r - 1]);
                            il.Emit(OpCodes.Ldloc, locals[r]);//EmitLoadLocal(il, locals[r]);
                            EmitCallSet(il, info.Info);
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
