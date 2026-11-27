using DataWF.Collections.Indexes;
using DataWF.Collections.Query;
using DataWF.Comparers;
using DataWF.Invokers;
using DataWF.Models;
using DataWF.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace DataWF.Invokers
{
    public static class InvokerExtensions
    {
        public static IQCompareParameter CreateTreeParameter(this IQueryInvoker invoker)
        {
            var parameter = invoker.CreateQParameter();
            parameter.RightItem = new QValue<bool>(true);
            parameter.IsGlobal = true;
            parameter.FormatIgnore = true;
            return parameter;
        }

        public static ITreeComparer CreateTreeComparer(this IInvoker invoker, ListSortDirection direction = ListSortDirection.Ascending)
        {
            return CreateTreeComparer(invoker, invoker.TargetType, direction);
        }

        public static ITreeComparer CreateTreeComparer(this IInvoker invoker, Type type, ListSortDirection direction = ListSortDirection.Ascending)
        {
            type = type ?? invoker.TargetType;
            return (ITreeComparer)Activator.CreateInstance(typeof(GroupComparer<>).MakeGenericType(type));
        }

        public static ITreeComparer<T> CreateTreeComparer<T>(this IInvoker invoker, ListSortDirection direction = ListSortDirection.Ascending) where T : IGroup
        {
            return (ITreeComparer<T>)CreateTreeComparer(invoker, typeof(T), direction);
        }

        public static bool Compare<V>(this IInvokerGetter<V> invoker, object item, CompareOperator compareOperator, V value)
        {
            return compareOperator.GetEvaluator<V>().Eval(invoker.GetValue(item), value, UniversalComparer<V>.Default);
        }

        public static bool Compare<V>(this IInvokerGetter<V> invoker, object item, CompareOperator compareOperator, object value)
        {
            return compareOperator.GetEvaluator<V>().Eval(invoker.GetValue(item), value, UniversalComparer<V>.Default);
        }

        public static bool Compare<T, V>(this IInvokerGetter<T, V> invoker, T item, CompareOperator compareOperator, V value)
        {
            return compareOperator.GetEvaluator<V>().Eval(invoker.GetValue(item), value, UniversalComparer<V>.Default);
        }

        public static bool Compare<T, V>(this IInvokerGetter<T, V> invoker, T item, CompareOperator compareOperator, object value)
        {
            return compareOperator.GetEvaluator<V>().Eval(invoker.GetValue(item), value, UniversalComparer<V>.Default);
        }

        public static IEnumerable<T> Filter<T, V>(this IQueryInvoker<V> invoker, IEnumerable<T> list, CompareOperator compareOperator, V value)
        {
            var compareParameter = new QCompareParameter(invoker)
            {
                Comparer = compareOperator,
                RightItem = invoker.CreateQValue(value)
            };
            return compareParameter.Filter<T>(list);
        }

        public static IEnumerable<T> Filter<T, V>(this IQueryInvoker<V> invoker, IEnumerable<T> list, CompareOperator compareOperator, object value)
        {
            var compareParameter = new QCompareParameter(invoker, compareOperator, value);
            return compareParameter.Filter<T>(list);
        }

        public static IEnumerable<T> Filter<T, V>(this IQueryInvoker<T, V> invoker, IEnumerable<T> list, CompareOperator compareOperator, V value)
        {
            var compareParameter = new QCompareParameter(invoker, compareOperator, value);
            return compareParameter.Filter<T>(list);
        }

        public static IEnumerable<T> Filter<T, V>(this IQueryInvoker<T, V> invoker, IEnumerable<T> list, CompareOperator compareOperator, object value)
        {
            var compareParameter = new QCompareParameter(invoker, compareOperator, value);
            return compareParameter.Filter<T>(list);
        }

        public static Expression GetExpression(IModelColumn propertyInvoker)
            => GetExpression(propertyInvoker, propertyInvoker.Invoker.Name);

        public static Expression GetExpression(IModelColumn propertyInvoker, string parameterName)
        {
            var param = Expression.Parameter(propertyInvoker.Invoker.TargetType, parameterName);
            return GetExpression(propertyInvoker, param);
        }

        public static Expression GetExpression(IModelColumn propertyInvoker, Expression instanceExpression)
        {
            var info = propertyInvoker.PropertyInfo;
            return Expression.Property(info.GetMethod.IsStatic ? null : instanceExpression, info);
        }

        public static IElementSerializer GetSerializer(this IInvoker invoker)
        {
            return invoker is IModelColumn propertyInvoker
                ? propertyInvoker.Serializer
                : Serialization.SerializeProvider.GetSerializer(invoker.TargetType);
        }
    }
}
