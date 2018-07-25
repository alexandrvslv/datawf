using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq.Expressions;

namespace DataWF.Common
{
    public delegate V GetHandler<T, V>(T target);
    public delegate void SetHandler<T, V>(ref T target, V value);

    public class RefPropertyInvoker<T, V> : IInvoker// where T : struct
    {
        public RefPropertyInvoker(PropertyInfo info)
        {
            Name = info.Name;
            CanWrite = info.CanWrite && info.GetSetMethod() != null;
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

        public Type TargetType { get { return typeof(T); } }

        internal GetHandler<T, V> GetAction { get; private set; }

        internal SetHandler<T, V> SetAction { get; private set; }

        public IListIndex CreateIndex()
        {
            throw new NotImplementedException();
        }

        public V Get(ref T target)
        {
            return GetAction(target);
        }

        public object GetValue(object target)
        {
            T item = (T)target;
            return Get(ref item);
        }

        public void Set(ref T target, V value)
        {
            SetAction(ref target, value);
        }

        public void SetValue(object target, object value)
        {
            T item = (T)target;
            Set(ref item, (V)value);
            target = (object)item;
        }

        private GetHandler<T, V> GetInvokerGet(MethodInfo info)
        {
            var method = new DynamicMethod(EmitInvoker.GetMethodName(info),
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

        private SetHandler<T, V> GetInvokerSet(MethodInfo info)
        {
            //DynamicMethod method = new DynamicMethod(EmitInvoker.GetMethodName(info),
            //                                         typeof(void),
            //                                         new Type[] { typeof(T).MakeByRefType(), typeof(V) },
            //                                         true);

            //var il = method.GetILGenerator();
            //if (!info.IsStatic)
            //{
            //    il.Emit(OpCodes.Ldarg_0);
            //    //il.Emit(OpCodes.Ldind_Ref);
            //}
            //il.Emit(OpCodes.Ldarg_1);
            //il.EmitCall(OpCodes.Call, info, null);
            //il.Emit(OpCodes.Ret);
            //return (SetHandler<T, V>)method.CreateDelegate(typeof(SetHandler<T, V>));

            var par1 = Expression.Parameter(typeof(T).MakeByRefType());
            var par2 = Expression.Parameter(typeof(V));
            return Expression.Lambda<SetHandler<T, V>>(
                Expression.Assign(Expression.Property(par1, info), par2),
                par1, par2
                ).Compile();
        }
    }

}
