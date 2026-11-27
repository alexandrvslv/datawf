using System;

namespace DataWF.Collections.Query;

public readonly struct JoinOperator
{
    public const string StrInner = "inner";
    public const string StrLeft = "left";
    public const string StrRight = "right";
    public const string StrOuter = "outer";
    public const string StrJoin = "join";
    public static readonly JoinOperator Undefined = new(JoinOperatorType.Undefined);
    public static readonly JoinOperator Join = new(JoinOperatorType.Join);
    public static readonly JoinOperator Inner = new(JoinOperatorType.Inner | JoinOperatorType.Join);
    public static readonly JoinOperator Left = new(JoinOperatorType.Left | JoinOperatorType.Join);
    public static readonly JoinOperator LeftOuter = new(JoinOperatorType.Left | JoinOperatorType.Outer | JoinOperatorType.Join);
    public static readonly JoinOperator Right = new(JoinOperatorType.Right | JoinOperatorType.Join);
    public static readonly JoinOperator RightOuter = new(JoinOperatorType.Right | JoinOperatorType.Outer | JoinOperatorType.Join);

    public static bool operator ==(JoinOperator typeLeft, JoinOperator typeRight)
    {
        return typeLeft.Equals(typeRight);
    }

    public static bool operator !=(JoinOperator typeLeft, JoinOperator typeRight)
    {
        return !typeLeft.Equals(typeRight);
    }

    public static string Format(JoinOperatorType type)
    {
        string rezult = string.Empty;
        if ((type & JoinOperatorType.Inner) == JoinOperatorType.Inner)
            rezult = StrInner;
        else if ((type & JoinOperatorType.Left) == JoinOperatorType.Left)
            rezult = StrLeft;
        else if ((type & JoinOperatorType.Right) == JoinOperatorType.Right)
            rezult = StrRight;

        if ((type & JoinOperatorType.Outer) == JoinOperatorType.Outer)
            rezult += string.Concat(" ", StrOuter);

        if (type != JoinOperatorType.Undefined)
            rezult += string.Concat(" ", StrJoin);
        return rezult;
    }

    public static JoinOperatorType Parse(ReadOnlySpan<char> code)
    {
        JoinOperatorType type = JoinOperatorType.Undefined;
        if (code.Equals(StrJoin.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = JoinOperatorType.Join;
        else if (code.Equals(StrInner.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = JoinOperatorType.Inner;
        else if (code.Equals(StrLeft.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = JoinOperatorType.Left;
        else if (code.Equals(StrRight.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = JoinOperatorType.Right;
        else if (code.Equals(StrOuter.AsSpan(), StringComparison.OrdinalIgnoreCase))
            type = JoinOperatorType.Outer;
        return type;
    }

    public static JoinOperatorType Parse(string code)
    {
        JoinOperatorType type = JoinOperatorType.Undefined;
        if (string.Equals(code, StrJoin, StringComparison.OrdinalIgnoreCase))
            type = JoinOperatorType.Join;
        else if (string.Equals(code, StrInner, StringComparison.OrdinalIgnoreCase))
            type = JoinOperatorType.Inner;
        else if (string.Equals(code, StrLeft, StringComparison.OrdinalIgnoreCase))
            type = JoinOperatorType.Left;
        else if (string.Equals(code, StrRight, StringComparison.OrdinalIgnoreCase))
            type = JoinOperatorType.Right;
        else if (string.Equals(code, StrOuter, StringComparison.OrdinalIgnoreCase))
            type = JoinOperatorType.Outer;
        return type;
    }

    public readonly JoinOperatorType Type;

    public bool IsInner => (Type & JoinOperatorType.Inner) == JoinOperatorType.Inner
                        || !IsLeft && !IsRight;

    public bool IsLeft => (Type & JoinOperatorType.Left) == JoinOperatorType.Left;

    public bool IsRight => (Type & JoinOperatorType.Right) == JoinOperatorType.Right;

    public JoinOperator(JoinOperatorType type)
    {
        Type = type;
    }

    public bool Equals(JoinOperator obj)
    {
        return Type == obj.Type;
    }

    public override bool Equals(object? obj)
    {
        return obj is JoinOperator joinType && Equals(joinType);
    }

    public override int GetHashCode()
    {
        return Type.GetHashCode();
    }

    public string Format()
    {
        return Format(Type);
    }

    public override string ToString()
    {
        return Locale.Get("JoinType", Format());
    }
}

