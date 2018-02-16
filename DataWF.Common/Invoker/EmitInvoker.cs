using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Linq;

namespace DataWF.Common
{
    /// <summary>
    /// EmitInvoker.
    /// Dynamic method + delegate container 
    /// for System.Reflection property, method and constru
    /// <summary>
    /// EmitInvoker.
    /// Dynamic method + delegate container 
    /// for System.Reflection property, method and constructor.
    /// <see href="http://www.codeproject.com/KB/cs/FastMethodInvoker.aspx"/>
    /// <see href="http://www.codeproject.com/KB/cs/FastInvokerWrapper.aspx"/>
    /// </summary>
    public static class EmitInvoker
    {
        private static Dictionary<long, IInvoker> cacheInvokers = new Dictionary<long, IInvoker>(1000);
        private static Dictionary<long, EmitConstructor> cacheCtors = new Dictionary<long, EmitConstructor>(1000);
        public static void DeleteCache()
        {
            cacheInvokers.Clear();
        }

        public static void DeleteCache(Type type)
        {
            DeleteCache(type, Type.EmptyTypes);

            var props = TypeHelper.GetPropertyes(type, false);
            foreach (var info in props)
                DeleteCache(info);

            var methods = type.GetMethods();
            foreach (var info in methods)
                DeleteCache(info);
        }

        public static void DeleteCache(Type type, params Type[] param)
        {
            ConstructorInfo info = type.GetConstructor(param);
            if (info != null)
                DeleteCache(info);
        }

        public static void DeleteCache(MemberInfo info)
        {
            int token = GetToken(info);
            cacheInvokers.Remove(token);
        }

        public static int GetToken(MemberInfo info)
        {
            return info.GetHashCode();
        }

        public static IInvoker Initialize<T>(string property)
        {
            return Initialize(typeof(T), property);
        }

        public static IInvoker<T, V> Initialize<T, V>(string property)
        {
            return (IInvoker<T, V>)Initialize(typeof(T), property);
        }

        public static IInvoker Initialize(Type type, string property)
        {
            var list = TypeHelper.GetMemberInfoList(type, property);
            if (list.Count == 1)
            {
                return Initialize(list[0], true);
            }
            if (list.Count > 1)
            {
                MemberInfo last = list.Last();
                var emittype = typeof(ComplexInvoker<,>).MakeGenericType(type, TypeHelper.GetMemberType(last));
                return (IInvoker)CreateObject(emittype, new[] { typeof(string), typeof(List<MemberInfo>) }, new object[] { property, list }, true);
            }
            return null;
        }

        public static EmitConstructor Initialize(Type type, Type[] param, bool cache = true)
        {
            var info = type.GetConstructor(param);
            if (info == null)
                return null;
            return Initialize(info, cache);
        }

        public static EmitConstructor Initialize(ConstructorInfo info, bool cache = true)
        {
            var token = GetToken(info);
            if (cache && cacheCtors.TryGetValue(token, out var invoker))
                return invoker;
            return cacheCtors[token] = new EmitConstructor(info);
        }

        public static IInvoker Initialize(MemberInfo info, bool cache)
        {
            if (info == null)
                return null;
            var token = GetToken(info);
            if (cache && cacheInvokers.TryGetValue(token, out var invoker))
                return invoker;

            return cacheInvokers[token] = Initialize(info, null);
        }

        public static IInvoker Initialize(MemberInfo info, object index = null)
        {
            int token = GetToken(info);
            IInvoker result = null;
            if (info is FieldInfo)
            {
                var type = typeof(FieldInvoker<,>).MakeGenericType(info.DeclaringType,
                                                                       ((FieldInfo)info).FieldType);
                result = (IInvoker)CreateObject(type, new[] { typeof(FieldInfo) }, new[] { info }, true);
            }
            else if (info is PropertyInfo)
            {
                var parameters = ((PropertyInfo)info).GetIndexParameters();
                if (parameters.Length == 0)
                {
                    if (info.DeclaringType.IsValueType)
                    {
                        var type = typeof(RefPropertyInvoker<,>).MakeGenericType(info.DeclaringType,
                                                                                  ((PropertyInfo)info).PropertyType);
                        result = (IInvoker)CreateObject(type, new[] { typeof(PropertyInfo) }, new[] { info }, true);
                    }
                    else
                    {
                        var type = typeof(PropertyInvoker<,>).MakeGenericType(info.DeclaringType,
                                                                                  ((PropertyInfo)info).PropertyType);
                        result = (IInvoker)CreateObject(type, new[] { typeof(PropertyInfo) }, new[] { info }, true);
                    }
                }
                else
                {
                    var type = typeof(IndexPropertyInvoker<,,>).MakeGenericType(info.DeclaringType,
                                                                                    ((PropertyInfo)info).PropertyType,
                                                                                    parameters[0].ParameterType);
                    result = (IInvoker)CreateObject(type, new[] { typeof(PropertyInfo), parameters[0].ParameterType }, new[] { info, index }, true);
                }
            }
            else if (info is MethodInfo)
            {
                if (info.Name == nameof(Object.ToString))
                {
                    result = new ToStringInvoker();
                }
                else
                {
                    var type = typeof(MethodInvoker<,>).MakeGenericType(info.DeclaringType,
                                                                            ((MethodInfo)info).ReturnType);
                    result = (IInvoker)CreateObject(type, new[] { typeof(MethodInfo) }, new[] { info }, true);
                }
            }
            return result;
        }

        //private static void Initialize(MethodInfo getInfo, MethodInfo setInfo, object index = null)
        //{
        //	mInfo = getInfo;
        //	type = getInfo.ReturnType;
        //	mIndex = index;
        //	if (getInfo != null)
        //	{
        //		if (!cFieldGet.TryGetValue(GetToken(getInfo), out getHandler))
        //			cFieldGet[GetToken(getInfo)] = getHandler = GetInvokerGet(getInfo);
        //	}
        //	if (setInfo != null)
        //	{
        //		write = true;
        //		if (!cFieldSet.TryGetValue(GetToken(setInfo), out setHandler))
        //			cFieldSet[GetToken(setInfo)] = setHandler = GetInvokerSet(setInfo);
        //	}
        //}
        public static int CompareKey(string member, object x, object key, IComparer comparer)
        {
            return CompareKey(Initialize(x.GetType(), member), x, key, comparer);
        }

        public static int CompareKey(IInvoker accesor, object x, object key, IComparer comparer)
        {
            return ListHelper.Compare(accesor.Get(x), key, comparer, false);
        }

        public static object CreateObject(Type type, bool cache = true)
        {
            return CreateObject(type, Type.EmptyTypes, null, cache);
        }

        public static object CreateObject(Type type, Type[] ctypes, object[] cparams, bool cache)
        {
            if (type == null)
                return null;

            return Initialize(type, ctypes, cache)?.Create(cparams);
        }

        public static object Invoke(Type type, string name, object item, params object[] parameters)
        {
            var types = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                types[i] = parameters[i].GetType();

            return Invoke((MethodInfo)TypeHelper.GetMemberInfo(type, name, false, types), item, parameters);
        }

        public static object Invoke(MethodInfo info, object item, params object[] pars)
        {
            if (info == null)
                return null;
            return ((IIndexInvoker)Initialize(info, true))?.Get(item, pars);
        }

        public static object GetValue(Type type, string name, object item)
        {
            return GetValue(TypeHelper.GetMemberInfo(type, name, false), item);
        }

        public static object GetValue(MemberInfo info, object item)
        {
            if (info == null)
                return null;
            return Initialize(info, true)?.Get(item);
        }

        public static object GetValue(MemberInfo info, object item, object index)
        {
            if (info == null)
                return null;
            return ((IIndexInvoker)Initialize(info, true))?.Get(item, index);
        }

        public static void SetValue(Type type, string name, object item, object value)
        {
            SetValue(TypeHelper.GetMemberInfo(type, name, false), item, value);
        }

        public static void SetValue(MemberInfo info, object item, object value)
        {
            var invoker = Initialize(info, true);
            if (invoker.CanWrite)
                invoker.Set(item, value);
        }

        public static void SetValue(MemberInfo info, object item, object value, object index)
        {
            ((IIndexInvoker)Initialize(info, true))?.Set(item, index, value);
        }

        public static string GetMethodName(MethodInfo info)
        {
            return string.Format("Dynamic{0}{1}{2}", info.DeclaringType.Name, info.Name, info.IsGenericMethod ? "G" : "");
        }

        public static LocalBuilder[] DeclareLocal(ILGenerator il, ParameterInfo[] ps, int index)
        {
            LocalBuilder[] locals = new LocalBuilder[ps.Length > index ? ps.Length - (index) : 0];
            for (int i = index; i < ps.Length; i++)
            {
                int j = i - index;
                locals[j] = il.DeclareLocal(ps[i].ParameterType);
                if (ps[i].DefaultValue is Enum)
                    EmitFastInt(il, (int)ps[i].DefaultValue);
                else if (ps[i].DefaultValue is int)
                    EmitFastInt(il, (int)ps[i].DefaultValue);
                else if (ps[i].DefaultValue is string)
                    il.Emit(OpCodes.Ldstr, (string)ps[i].DefaultValue);
                else if (ps[i].DefaultValue == null)
                    il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Stloc, locals[j]);
            }
            return locals;
        }


        public static void EmitCastToReference(ILGenerator il, System.Type type)
        {
            if (type.IsValueType)
                il.Emit(OpCodes.Unbox_Any, type);
            else
                il.Emit(OpCodes.Castclass, type);
        }

        public static void EmitBoxIfNeeded(ILGenerator il, System.Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
        }

        public static void EmitFastInt(ILGenerator il, int value)
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
    }

    public delegate object TaskAction();

    public class TaskExecutor
    {
        private string name;
        private Thread thread;
        public ThreadStart Start;

        public TaskAction Action { get; set; }
        public object Object { get; set; }
        public object Tag { get; set; }
        public TimeSpan Time { get; set; }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public bool IsAsynchStart
        {
            get { return thread != null; }
        }

        public bool IsAsynchComplete
        {
            get
            {
                if (thread != null)
                    return thread.ThreadState == ThreadState.Stopped;
                return false;
            }
        }

        public event Action<RProcedureEventArgs> Callback;

        public object Execute()
        {
            object result = null;
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                result = Action();
                watch.Stop();
                Time = watch.Elapsed;
                OnCallback(result);
            }
            catch (Exception e)
            {
                OnCallback(e);
            }
            return result;

        }

        public void ExecuteAsynch()
        {
            if (thread == null)
            {
                if (Start == null)
                    Start = new ThreadStart(() => Execute());
                thread = new Thread(Start);
            }
            thread.Start();
        }

        public void Cancel()
        {
            if (thread != null && !name.StartsWith("Cancelation!", StringComparison.OrdinalIgnoreCase))
            {
                name = "Cancelation! " + Name;
                thread.Abort();
            }
        }

        public void OnCallback(object result)
        {
            if (Callback != null)
                Callback(new RProcedureEventArgs { Task = this, Result = result });
        }
    }

    public class RProcedureEventArgs : EventArgs
    {
        public TaskExecutor Task { get; set; }
        public object Result { get; set; }

    }

}
