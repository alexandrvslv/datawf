using DataWF.Collections.Query.Evaluators;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace DataWF.Collections.Query;

public readonly struct CompareOperator
{
    private const string StrIs = "is";
    private const string StrNot = "not";
    private const string StrNull = "null";
    private const string StrEquals = "=";
    private const string StrNotEquals = "!=";
    private const string StrNotEqualsV2 = "<>";
    private const string StrIn = "in";
    private const string StrNotIn = "not in";
    private const string StrIsNull = "is null";
    private const string StrIsNotNull = "is not null";
    private const string StrIsTrue = "is true";
    private const string StrIsNotTrue = "is not true";
    private const string StrIsFalse = "is false";
    private const string StrIsNotFalse = "is not false";
    private const string StrLike = "like";
    private const string StrNotLike = "not like";
    private const string StrBetween = "between";
    private const string StrNotBetween = "not between";
    private const string StrGreater = ">";
    private const string StrGreaterOrEqual = ">=";
    private const string StrLess = "<";
    private const string StrLessOrEqual = "<=";
    private const string StrTrue = "true";
    private const string StrFalse = "false";
    public const string StrAnd = "and";
    public const string StrAndCode = "&";
    public const string StrAndElse = "&&";
    public const string StrOr = "or";
    public const string StrOrCode = "|";
    public const string StrOrElse = "||";
    public const string StrAndNot = "and not";
    public const string StrOrNot = "or not";
    public static readonly CompareOperator Undefined = new(CompareOperatorType.Undefined);
    public static readonly CompareOperator Like = new(CompareOperatorType.Like);
    public static readonly CompareOperator NotLike = new(CompareOperatorType.NotLike);
    public static readonly CompareOperator Equal = new(CompareOperatorType.Equal);
    public static readonly CompareOperator NotEqual = new(CompareOperatorType.NotEqual);
    public static readonly CompareOperator In = new(CompareOperatorType.In);
    public static readonly CompareOperator NotIn = new(CompareOperatorType.NotIn);
    public static readonly CompareOperator IsNull = new(CompareOperatorType.IsNull);
    public static readonly CompareOperator IsNotNull = new(CompareOperatorType.IsNotNull);
    public static readonly CompareOperator Between = new(CompareOperatorType.Between);
    public static readonly CompareOperator NotBetween = new(CompareOperatorType.NotBetween);
    public static readonly CompareOperator Greater = new(CompareOperatorType.Greater);
    public static readonly CompareOperator GreaterOrEqual = new(CompareOperatorType.GreaterOrEqual);
    public static readonly CompareOperator Less = new(CompareOperatorType.Less);
    public static readonly CompareOperator LessOrEqual = new(CompareOperatorType.LessOrEqual);
    public static readonly CompareOperator IsTrue = new(CompareOperatorType.IsTrue);
    public static readonly CompareOperator IsFalse = new(CompareOperatorType.IsFalse);
    public static readonly CompareOperator IsNotTrue = new(CompareOperatorType.IsNotTrue);
    public static readonly CompareOperator IsNotFalse = new(CompareOperatorType.IsNotFalse);
    public static readonly CompareOperator LogicNot = new(CompareOperatorType.Not);
    public static readonly CompareOperator And = new(CompareOperatorType.And);
    public static readonly CompareOperator AndAlso = new(CompareOperatorType.AndAlso);
    public static readonly CompareOperator AndNot = new(CompareOperatorType.AndNot);
    public static readonly CompareOperator Or = new(CompareOperatorType.Or);
    public static readonly CompareOperator OrElse = new(CompareOperatorType.OrElse);
    public static readonly CompareOperator OrNot = new(CompareOperatorType.OrNot);


    public static bool operator ==(CompareOperator typeLeft, CompareOperator typeRight)
    {
        return typeLeft.Equals(typeRight);
    }

    public static bool operator !=(CompareOperator typeLeft, CompareOperator typeRight)
    {
        return !typeLeft.Equals(typeRight);
    }

    public static implicit operator CompareOperator(CompareOperatorType type) => new CompareOperator(type);

    public readonly CompareOperatorType Type;

    public CompareOperator(CompareOperatorType type)
    {
        Type = type;
    }

    public bool Not => (Type & CompareOperatorType.Not) == CompareOperatorType.Not;

    public ICompareEvaluator<V> GetEvaluator<V>() => GetEvaluator<V>(Type);

    public string Format() => Format(Type);

    public static ICompareEvaluator<V> GetEvaluator<V>(CompareOperatorType type)
    {
        return type switch
        {
            CompareOperatorType.Equal => CompareEqualEvaluator<V>.Instance,
            CompareOperatorType.In => CompareInEvaluator<V>.Instance,
            CompareOperatorType.Like => CompareLikeEvaluator<V>.Instance,
            CompareOperatorType.Between => CompareBetweenEvaluator<V>.Instance,
            CompareOperatorType.Greater => CompareGreaterEvaluator<V>.Instance,
            CompareOperatorType.Less => CompareLessEvaluator<V>.Instance,
            CompareOperatorType.GreaterOrEqual => CompareGreaterOrEqualEvaluator<V>.Instance,
            CompareOperatorType.LessOrEqual => CompareLessOrEqualEvaluator<V>.Instance,
            CompareOperatorType.IsNull => CompareIsNullEvaluator<V>.Instance,
            CompareOperatorType.IsNotNull => CompareIsNotNullEvaluator<V>.Instance,
            CompareOperatorType.IsTrue => CompareIsTrueEvaluator<V>.Instance,
            CompareOperatorType.IsNotTrue => CompareIsNotTrueEvaluator<V>.Instance,
            CompareOperatorType.IsFalse => CompareIsFalseEvaluator<V>.Instance,
            CompareOperatorType.IsNotFalse => CompareIsNotFalseEvaluator<V>.Instance,
            CompareOperatorType.NotEqual => CompareNotEqualEvaluator<V>.Instance,
            CompareOperatorType.NotIn => CompareNotInEvaluator<V>.Instance,
            CompareOperatorType.NotLike => CompareNotLikeEvaluator<V>.Instance,
            CompareOperatorType.NotBetween => CompareNotBetweenEvaluator<V>.Instance,
            CompareOperatorType.AndAlso => (ICompareEvaluator<V>)LogicAndAlsoEvaluator.Instance,
            CompareOperatorType.And => (ICompareEvaluator<V>)LogicAndEvaluator.Instance,
            CompareOperatorType.OrElse => (ICompareEvaluator<V>)LogicOrElseEvaluator.Instance,
            CompareOperatorType.Or => (ICompareEvaluator<V>)LogicOrEvaluator.Instance,
            CompareOperatorType.AndNot => (ICompareEvaluator<V>)LogicAndNotEvaluator.Instance,
            CompareOperatorType.OrNot => (ICompareEvaluator<V>)LogicOrNotEvaluator.Instance,
            _ => null!
        };
    }

    public static string Format(CompareOperatorType type)
    {
        return type switch
        {
            CompareOperatorType.Equal => StrEquals,
            CompareOperatorType.NotEqual => StrNotEquals,
            CompareOperatorType.In => StrIn,
            CompareOperatorType.NotIn => StrNotIn,
            CompareOperatorType.IsNull => StrIsNull,
            CompareOperatorType.IsNotNull => StrIsNotNull,
            CompareOperatorType.Like => StrLike,
            CompareOperatorType.NotLike => StrNotLike,
            CompareOperatorType.Between => StrBetween,
            CompareOperatorType.NotBetween => StrNotBetween,
            CompareOperatorType.Greater => StrGreater,
            CompareOperatorType.GreaterOrEqual => StrGreaterOrEqual,
            CompareOperatorType.Less => StrLess,
            CompareOperatorType.LessOrEqual => StrLessOrEqual,
            CompareOperatorType.IsTrue => StrIsTrue,
            CompareOperatorType.IsFalse => StrIsFalse,
            CompareOperatorType.IsNotTrue => StrIsNotTrue,
            CompareOperatorType.IsNotFalse => StrIsNotFalse,
            _ => string.Empty,
        };
    }

    public override string ToString() => Locale.Get("CompareType", Format());

    public bool Equals(CompareOperator obj) => Not == obj.Not && Type == obj.Type;

    public override bool Equals(object? obj) => obj is CompareOperator compare && Equals(compare);

    public override int GetHashCode() => Type.GetHashCode();

    public static CompareOperatorType Parse(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => CompareOperatorType.Equal,
            ExpressionType.NotEqual => CompareOperatorType.NotEqual,
            ExpressionType.GreaterThan => CompareOperatorType.Greater,
            ExpressionType.GreaterThanOrEqual => CompareOperatorType.GreaterOrEqual,
            ExpressionType.LessThanOrEqual => CompareOperatorType.LessOrEqual,
            ExpressionType.LessThan => CompareOperatorType.Less,
            ExpressionType.Or => CompareOperatorType.Or,
            ExpressionType.OrElse => CompareOperatorType.OrElse,
            ExpressionType.And => CompareOperatorType.And,
            ExpressionType.AndAlso => CompareOperatorType.AndAlso,
            _ => CompareOperatorType.Undefined,
        };
    }

    public static CompareOperatorType Parse(ReadOnlySpan<char> code)
    {
        CompareOperatorType type = CompareOperatorType.Undefined;
        if (MemoryExtensions.Equals(code, StrAnd.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.AndAlso;
        else if (MemoryExtensions.Equals(code, StrOr.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.OrElse;
        else if (MemoryExtensions.Equals(code, StrAndElse.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.AndAlso;
        else if (MemoryExtensions.Equals(code, StrOrElse.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.OrElse;
        else if (MemoryExtensions.Equals(code, StrEquals.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Equal;
        else if (MemoryExtensions.Equals(code, StrGreater.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Greater;
        else if (MemoryExtensions.Equals(code, StrGreaterOrEqual.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.GreaterOrEqual;
        else if (MemoryExtensions.Equals(code, StrLess.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Less;
        else if (MemoryExtensions.Equals(code, StrLessOrEqual.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.LessOrEqual;
        else if (MemoryExtensions.Equals(code, StrIn.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.In;
        else if (MemoryExtensions.Equals(code, StrLike.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Like;
        else if (MemoryExtensions.Equals(code, StrBetween.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Between;
        else if (MemoryExtensions.Equals(code, StrIs.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Is;
        else if (MemoryExtensions.Equals(code, StrNot.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Not;
        else if (MemoryExtensions.Equals(code, StrNull.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Null;
        else if (MemoryExtensions.Equals(code, StrIn.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.In;
        else if (MemoryExtensions.Equals(code, StrTrue.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.True;
        else if (MemoryExtensions.Equals(code, StrFalse.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.False;
        return type;
    }

    public static CompareOperatorType Parse(string code)
    {
        CompareOperatorType type = CompareOperatorType.Undefined;
        if (string.Equals(code, StrAnd, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.AndAlso;
        else if (string.Equals(code, StrOr, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.OrElse;
        else if (string.Equals(code, StrAndElse, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.AndAlso;
        else if (string.Equals(code, StrOrElse, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.OrElse;
        else if (string.Equals(code, StrIs, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Is;
        else if (string.Equals(code, StrNot, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Not;
        else if (string.Equals(code, StrNull, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Null;
        else if (string.Equals(code, StrEquals, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Equal;
        else if (string.Equals(code, StrGreater, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Greater;
        else if (string.Equals(code, StrGreaterOrEqual, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.GreaterOrEqual;
        else if (string.Equals(code, StrLess, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Less;
        else if (string.Equals(code, StrLessOrEqual, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.LessOrEqual;
        else if (string.Equals(code, StrIn, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.In;
        else if (string.Equals(code, StrLike, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Like;
        else if (string.Equals(code, StrBetween, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.Between;
        else if (string.Equals(code, StrIn, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.In;
        else if (string.Equals(code, StrTrue, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.True;
        else if (string.Equals(code, StrFalse, StringComparison.OrdinalIgnoreCase))
            type = CompareOperatorType.False;
        return type;
    }

    public static CompareOperator ParseFull(string value)
    {
        var compareSplit = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (compareSplit.Length > 1)
        {
            var type = CompareOperatorType.Undefined;
            foreach (var stype in compareSplit.Select(p => Parse(p)))
                type |= stype;
            var compareValue = string.Equals(compareSplit[0], StrNot, StringComparison.OrdinalIgnoreCase) ? compareSplit[1] : compareSplit[0];
            return new CompareOperator(type);
        }
        else if (compareSplit[0][0] == '!')
        {
            return new CompareOperator(CompareOperatorType.Not | Parse(compareSplit[0].Substring(1)));
        }

        return new CompareOperator(Parse(compareSplit[0]));
    }
}


