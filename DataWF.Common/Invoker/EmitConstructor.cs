using System;
using System.Reflection.Emit;
using System.Reflection;

namespace DataWF.Common
{
    public class EmitConstructor
    {

        public EmitConstructor(ConstructorInfo info)
            : base()
        {
            Info = info;
            CreateAction = GetConstructorInvoker(info);
        }

        public EmitConstructor(Type type, params Type[] argument)
            : this(type.GetConstructor(argument))
        { }

        internal Func<object[], object> CreateAction { get; private set; }

        public ConstructorInfo Info { get; private set; }

        public object Create(params object[] target)
        {
            return CreateAction(target);
        }

        private static Func<object[], object> GetConstructorInvoker(ConstructorInfo info)
        {
            var dynamicMethod = new DynamicMethod(
                string.Format("Dynamic{0}{1}{2}", info.DeclaringType.Name.Replace("`1", ""), "Ctor", info.IsGenericMethod ? "G" : ""),
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
                EmitInvoker.EmitFastInt(il, i);
                il.Emit(OpCodes.Ldelem_Ref);
                EmitInvoker.EmitCastToReference(il, ps[i].ParameterType);
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


    }
}
