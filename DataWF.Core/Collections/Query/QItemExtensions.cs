//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using DataWF.Collections.Indexes;
using DataWF.Invokers;
using DataWF.Models;

namespace DataWF.Collections.Query;

public static class QItemExtensions
{
    public static IQParameter CreateQParameter(this IQueryInvoker invoker, CompareOperator compareOperator, object value, IEnumerable source)
    {
        return new QCompareParameter
        {
            LeftItem = CreateItem(invoker, invoker, compareOperator.Type, source),
            Comparer = compareOperator,
            RightItem = CreateItem(value, invoker, compareOperator.Type, source)
        };
    }

    public static IQItem CreateItem(object value, IQCompareParameter parameter)
    {
        return CreateItem(value, (parameter.LeftItem as IQInvoker)?.Invoker, parameter.Comparer.Type, parameter.LeftItem?.QTable?.Source, parameter.LeftItem?.QTable);
    }

    public static IQItem CreateItem(object? value, IQueryInvoker? property, CompareOperatorType compareOperator = CompareOperatorType.Undefined, IEnumerable source = null, IQTable qTable = null)
    {
        IQItem result = value switch
        {
            IQItem qItem => qItem,
            IIndex listIndex => listIndex.CreateQIndex(qTable?.TableAlias),
            IInvoker pInvoker when
                source is IIndexList indexedSource
                && indexedSource.GetIndex(pInvoker) is IIndex listIndex1 => listIndex1.CreateQIndex(qTable?.TableAlias),
            IModelColumn pInvoker1 => pInvoker1.CreateQInvoker(qTable?.TableAlias),
            IQueryInvoker vInvoker => vInvoker.CreateQInvoker(qTable?.TableAlias),
            IBetween betweenValue => property.CreateQBetween(betweenValue.GetMin(), betweenValue.GetMax()),
            string text => CreateConst(text, property, compareOperator),
            byte[] bytes => CreateConst(bytes, property, compareOperator),
            IEnumerable enumerable => new QArray(enumerable, property, compareOperator),
            _ => CreateConst(value, property, compareOperator),
        };
        if (qTable != null)
            result.QTable = qTable;
        return result;
    }

    public static IQValue CreateConst(object value, IQueryInvoker? invoker = null, CompareOperatorType compareOperator = CompareOperatorType.Undefined)
    {
        if (value is string stringValue)
            return CreateConst(stringValue, invoker, compareOperator);
        if (invoker != null)
        {
            if (value?.GetType() == invoker.DataType)
                return invoker.CreateQValue(value);
            if (value is IPrimaryKey dbItem)
                return invoker.CreateQValue(dbItem.PrimaryKey);
            return invoker.CreateQValue(value);
        }
        return (IQValue)InvokeProvider.CreateObject(typeof(QValue<>).MakeGenericType(value.GetType()), new Type[] { value.GetType() }, new[] { value });
    }

    public static IQValue CreateConst(string value, IQueryInvoker? invoker = null, CompareOperatorType compareOperator = CompareOperatorType.Undefined)
    {
        if (invoker == null)
        {
            if ((compareOperator & CompareOperatorType.Like) == CompareOperatorType.Like)
            {
                if (value.IndexOf('%') < 0)
                    value = $"%{value}%";
                return new QObjectValue(Helper.BuildLike(value));
            }
            if ((compareOperator & CompareOperatorType.In) == CompareOperatorType.In)
            {
                return new QObjectValue(value.Split(Helper.CommaSeparator, StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                DateTime date;
                if (DateTime.TryParse((string)value, out date))
                    return new QValue<DateTime>(date);
                else if (DateTime.TryParseExact((string)value, new string[] { "yyyyMMdd" }, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None, out date))
                    return new QValue<DateTime>(date);
            }
            return new QValue<string>(value);
        }
        else
        {
            if ((compareOperator & CompareOperatorType.Like) == CompareOperatorType.Like)
            {
                if (value.IndexOf('%') < 0)
                    value = $"%{value}%";
                return new QObjectValue(Helper.BuildLike(value));
            }
            if ((compareOperator & CompareOperatorType.In) == CompareOperatorType.In)
            {
                var splitValue = value.Split(Helper.CommaSeparator, StringSplitOptions.RemoveEmptyEntries);
                if (invoker.DataType == typeof(string))
                    return new QObjectValue(splitValue);
                else
                    return new QObjectValue(splitValue.Select(x => invoker.GetSerializer().FromString(x)));

            }
            if (invoker.DataType == typeof(string))
                return new QValue<string>(value);
            return invoker.CreateQValue(invoker.GetSerializer().FromString(value));
        }
    }

    public static IQItem CreateColumn(this IQQuery query, Expression expression)
    {
        if (expression is MemberExpression memberExpression)
        {
            var column = query.ParseInvoker(memberExpression.Member.Name, null, out var qTable);
            if (column != null)
            {
                return CreateColumn(column, qTable.TableAlias);
            }
            else
            {
                var invoker = (IQueryInvoker)InvokeProvider.GetInvoker(memberExpression.Member);
                return invoker.CreateQInvoker(query.GetTable(memberExpression.Member.ReflectedType)?.TableAlias);
            }
        }
        else
        {
            throw new NotSupportedException(expression.GetType().Name);
        }
    }

    private static IQItem CreateColumn(this IQueryInvoker invoker, string prefix)
    {
        if (invoker is IQItem qItem)
            return qItem;
        else
            return invoker.CreateQInvoker(prefix);
    }

    public static QOrder CreateOrderBy(this IQQuery query, Expression expression, ListSortDirection direction)
    {
        if (expression is MemberExpression memberExpression)
        {
            var column = query.ParseInvoker(memberExpression.Member.Name, null, out var qTable);
            if (column != null)
            {
                return CreateOrderBy(column, direction, qTable.TableAlias);
            }
            else
            {
                var invoker = (IQueryInvoker)InvokeProvider.GetInvoker(memberExpression.Member);
                return CreateOrderBy(invoker, direction, query.GetTable(memberExpression.Member.ReflectedType)?.TableAlias);
            }
        }
        else
        {
            throw new NotSupportedException(expression.GetType().Name);
        }
    }

    public static QOrder CreateOrderBy(IQueryInvoker invoker, ListSortDirection direction, string? tableAlias = null)
    {
        return new QOrder(invoker.CreateQInvoker(tableAlias))
        {
            Direction = direction
        };
    }

    public static IQItem ParseMember(this IQItem item, MemberExpression memberExpression)
    {
        var column = item.Query.ParseInvoker(memberExpression.Member.Name, null, out var qTable);
        if (column != null)
        {
            return CreateItem(column, null, CompareOperatorType.Undefined, qTable: qTable);
        }
        else
        {
            return CreateItem(InvokeProvider.GetInvoker(memberExpression.Member), null);
        }
    }

    public static IQItem ParsePartOfBaseExpression(this IQItem item, Expression expression, Expression baseExpression)
    {
        switch (expression)
        {
            case MemberExpression memberExpression:
                if (memberExpression.Expression is not ParameterExpression)
                    return item.ParsePartOfBaseExpression(memberExpression.Expression, expression);
                return item.ParseMember(memberExpression);
            case ConstantExpression constExpression:
                if (baseExpression is UnaryExpression
                    || baseExpression is BinaryExpression)
                    return CreateConst(constExpression.Value);
                else if (baseExpression is MemberExpression constMemberExpression)
                    return CreateConst(InvokeProvider.GetValue(constMemberExpression.Member, constExpression.Value));
                throw new NotImplementedException("TODO");
            case UnaryExpression unaryExpression:
                return item.ParsePartOfBaseExpression(unaryExpression.Operand, unaryExpression);
            case BinaryExpression binaryExpression:
                return item.ParseBinary(binaryExpression);
            default:
                throw new NotImplementedException("TODO");
        }
    }

    public static IQParameter ParseParameter(this IQItem item, Expression expression)
    {
        switch (expression)
        {
            case BinaryExpression binaryExpression:
                return (IQParameter)item.ParseBinary(binaryExpression);
            case MemberExpression memberExpression:
                return item.ParseParameter(memberExpression);
            case UnaryExpression unaryExpression:
                return item.ParseParameter(unaryExpression);
            default:
                throw new NotImplementedException("TODO");
        }
    }

    public static IQParameter ParseParameter(this IQItem item, UnaryExpression unaryExpression)
    {
        switch (unaryExpression.NodeType)
        {
            case ExpressionType.Not:
                if (unaryExpression.Operand is MemberExpression memberExpression)
                {
                    var param = (IQCompareParameter)item.ParseParameter(memberExpression);
                    param.Comparer = CompareOperator.NotEqual;
                    return param;
                }
                throw new NotImplementedException("TODO");
        }
        throw new NotImplementedException("TODO");
    }

    public static IQParameter ParseParameter(this IQItem item, MemberExpression memberExpression)
    {
        if (memberExpression.Member.GetMemberType() == typeof(bool))
        {
            return new QCompareParameter
            {
                LeftItem = item.ParseMember(memberExpression),
                Comparer = CompareOperator.Equal,
                RightItem = new QValue<bool>(true),
            };
        }
        throw new NotImplementedException("TODO");
    }

    public static IQBinaryItem ParseBinary(this IQItem item, BinaryExpression binaryExpression)
    {
        var compareOperator = CompareOperator.Parse(binaryExpression.NodeType);
        if (compareOperator != CompareOperatorType.Undefined)
        {
            var qParam = new QCompareParameter
            {
                Holder = item,
                Comparer = compareOperator
            };
            qParam.LeftItem = qParam.ParsePartOfBaseExpression(binaryExpression.Left, binaryExpression);
            qParam.RightItem = qParam.ParsePartOfBaseExpression(binaryExpression.Right, binaryExpression);
            return qParam;
        }
        else
        {
            var mathOperator = MathOperator.Parse(binaryExpression.NodeType);
            if (mathOperator != MathOperatorType.Undefined)
            {
                var qExpression = (IQMathBinary)InvokeProvider.CreateObject(typeof(QMathBinary<>).MakeGenericType(binaryExpression.Left.Type));
                qExpression.Holder = item;
                qExpression.MathOperator = mathOperator;
                qExpression.LeftItem = qExpression.ParsePartOfBaseExpression(binaryExpression.Left, binaryExpression);
                qExpression.RightItem = qExpression.ParsePartOfBaseExpression(binaryExpression.Right, binaryExpression);
                return qExpression;
            }
            else
            {
                throw new NotImplementedException("TODO");
            }
        }
    }
}

