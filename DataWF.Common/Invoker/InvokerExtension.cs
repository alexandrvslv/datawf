using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Common
{
    public static class InvokerExtension
    {
        //IListIndex CreateIndex(bool concurrent);
        //IListIndex CreateIndex<T>(bool concurrent);

        //IQueryParameter CreateParameter(Type type);
        //IQueryParameter CreateParameter(Type type, CompareType comparer, object value);
        //IQueryParameter CreateParameter(Type type, LogicType logic, CompareType comparer, object value = null, QueryGroup group = QueryGroup.None);

        //IQueryParameter<TT> CreateParameter<TT>(CompareType comparer, object value);
        //IQueryParameter<TT> CreateParameter<TT>(LogicType logic, CompareType comparer, object value = null, QueryGroup group = QueryGroup.None);

        //IComparer CreateComparer(Type type, ListSortDirection direction = ListSortDirection.Ascending);
        //IComparer<TT> CreateComparer<TT>(ListSortDirection direction = ListSortDirection.Ascending);


        public static IListIndex<T, V> CreateIndex<T, V>(this IValuedInvoker<V> invoker, bool concurrent)
        {
            return ListIndexFactory.Create<T, V>(invoker, concurrent);
        }

        public static IListIndex CreateIndex<T>(this IInvoker invoker, bool concurrent)
        {
            return ListIndexFactory.Create<T>(invoker, concurrent);
        }

        public static IQueryParameter CreateParameter(this IInvoker invoker)
        {
            return CreateParameter(invoker, invoker.TargetType);
        }

        public static IQueryParameter CreateParameter(this IInvoker invoker, Type type)
        {
            type = type ?? invoker.TargetType;
            return (IQueryParameter)Activator.CreateInstance(typeof(QueryParameter<,>).MakeGenericType(type, invoker.DataType), invoker);
        }

        public static IQueryParameter CreateParameter(this IInvoker invoker, Type type, CompareType compare, object value)
        {
            type = type ?? invoker.TargetType;
            var parameter = CreateParameter(invoker, type);
            parameter.Comparer = compare;
            parameter.Value = value;
            return parameter;
        }

        public static IQueryParameter CreateParameter(this IInvoker invoker, Type type, LogicType logic, CompareType compare, object value = null, QueryGroup group = QueryGroup.None)
        {
            type = type ?? invoker.TargetType;
            var parameter = CreateParameter(invoker, type);
            parameter.Logic = logic;
            parameter.Comparer = compare;
            parameter.Value = value;
            parameter.Group = group;
            return parameter;
        }

        public static IQueryParameter<T> CreateParameter<T>(this IInvoker invoker)
        {
            return CreateParameter<T>(invoker, CompareType.Equal, null);
        }

        public static IQueryParameter<T> CreateParameter<T>(this IInvoker invoker, CompareType compare, object value)
        {
            return (IQueryParameter<T>)invoker.CreateParameter(typeof(T), compare, value);
        }

        public static IQueryParameter<T> CreateParameter<T>(this IInvoker invoker, LogicType logic, CompareType compare, object value = null, QueryGroup group = QueryGroup.None)
        {
            return (IQueryParameter<T>)invoker.CreateParameter(typeof(T), logic, compare, value, group);
        }

        public static IComparer CreateComparer(this IInvoker invoker, ListSortDirection direction = ListSortDirection.Ascending)
        {
            return CreateComparer(invoker, invoker.TargetType, direction);
        }

        public static IComparer CreateComparer(this IInvoker invoker, Type type, ListSortDirection direction = ListSortDirection.Ascending)
        {
            type = type ?? invoker.TargetType;
            return (InvokerComparer)Activator.CreateInstance(typeof(InvokerComparer<,>).MakeGenericType(type, invoker.DataType), invoker, direction);
        }

        public static IComparer<T> CreateComparer<T>(this IInvoker invoker, ListSortDirection direction = ListSortDirection.Ascending)
        {
            return (IComparer<T>)invoker.CreateComparer(typeof(T), direction);
        }

        public static IQueryParameter CreateTreeParameter(this IInvoker invoker, Type type)
        {
            var parameter = invoker.CreateParameter(type);
            parameter.Value = true;
            parameter.IsGlobal = true;
            parameter.FormatIgnore = true;
            return parameter;
        }

        public static IQueryParameter<T> CreateTreeParameter<T>(this IInvoker invoker)
        {
            var parameter = invoker.CreateParameter<T>();
            parameter.Value = true;
            parameter.IsGlobal = true;
            parameter.FormatIgnore = true;
            return parameter;
        }

        public static IComparer CreateTreeComparer(this IInvoker invoker, Type type, ListSortDirection direction = ListSortDirection.Ascending)
        {
            type = type ?? invoker.TargetType;
            return (InvokerComparer)Activator.CreateInstance(typeof(TreeComparer<>).MakeGenericType(type));
        }

        public static IComparer<T> CreateTreeComparer<T>(this IInvoker invoker, ListSortDirection direction = ListSortDirection.Ascending)
        {
            return (InvokerComparer<T>)invoker.CreateTreeComparer(typeof(T), direction);
        }
    }
}
