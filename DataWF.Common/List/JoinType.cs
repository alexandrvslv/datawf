using System;

namespace DataWF.Common
{
    public readonly struct JoinType
    {
        public const string StrInner = "inner";
        public const string StrLeft = "left";
        public const string StrRight = "right";
        public const string StrOuter = "outer";
        public const string StrJoin = "join";
        public static readonly JoinType Undefined = new JoinType(JoinTypes.Undefined);
        public static readonly JoinType Join = new JoinType(JoinTypes.Join);
        public static readonly JoinType Inner = new JoinType(JoinTypes.Inner | JoinTypes.Join);
        public static readonly JoinType Left = new JoinType(JoinTypes.Left | JoinTypes.Join);
        public static readonly JoinType LeftOuter = new JoinType(JoinTypes.Left | JoinTypes.Outer | JoinTypes.Join);
        public static readonly JoinType Right = new JoinType(JoinTypes.Right | JoinTypes.Join);
        public static readonly JoinType RightOuter = new JoinType(JoinTypes.Right | JoinTypes.Outer | JoinTypes.Join);

        public static bool operator ==(JoinType typeLeft, JoinType typeRight)
        {
            return typeLeft.Equals(typeRight);
        }

        public static bool operator !=(JoinType typeLeft, JoinType typeRight)
        {
            return !typeLeft.Equals(typeRight);
        }

        public static string Format(JoinTypes type)
        {
            string rezult = string.Empty;
            if ((type & JoinTypes.Inner) == JoinTypes.Inner)
                rezult = StrInner;
            else if ((type & JoinTypes.Left) == JoinTypes.Left)
                rezult = StrLeft;
            else if ((type & JoinTypes.Right) == JoinTypes.Right)
                rezult = StrRight;

            if ((type & JoinTypes.Outer) == JoinTypes.Outer)
                rezult += string.Concat(" ", StrOuter);

            if (type != JoinTypes.Undefined)
                rezult += string.Concat(" ", StrJoin);
            return rezult;
        }

        public static JoinTypes Parse(ReadOnlySpan<char> code)
        {
            JoinTypes type = JoinTypes.Undefined;
            if (MemoryExtensions.Equals(code, StrJoin.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = JoinTypes.Join;
            else if (MemoryExtensions.Equals(code, StrInner.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = JoinTypes.Inner;
            else if (MemoryExtensions.Equals(code, StrLeft.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = JoinTypes.Left;
            else if (MemoryExtensions.Equals(code, StrRight.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = JoinTypes.Right;
            else if (MemoryExtensions.Equals(code, StrOuter.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = JoinTypes.Outer;
            return type;
        }

        public static JoinTypes Parse(string code)
        {
            JoinTypes type = JoinTypes.Undefined;
            if (string.Equals(code, StrJoin, StringComparison.OrdinalIgnoreCase))
                type = JoinTypes.Join;
            else if (string.Equals(code, StrInner, StringComparison.OrdinalIgnoreCase))
                type = JoinTypes.Inner;
            else if (string.Equals(code, StrLeft, StringComparison.OrdinalIgnoreCase))
                type = JoinTypes.Left;
            else if (string.Equals(code, StrRight, StringComparison.OrdinalIgnoreCase))
                type = JoinTypes.Right;
            else if (string.Equals(code, StrOuter, StringComparison.OrdinalIgnoreCase))
                type = JoinTypes.Outer;
            return type;
        }

        public readonly JoinTypes Type;

        public bool IsInner => (Type & JoinTypes.Inner) == JoinTypes.Inner
                            || (!IsLeft && !IsRight);

        public bool IsLeft => (Type & JoinTypes.Left) == JoinTypes.Left;

        public bool IsRight => (Type & JoinTypes.Right) == JoinTypes.Right;

        public JoinType(JoinTypes type)
        {
            Type = type;
        }

        public bool Equals(JoinType obj)
        {
            return Type == obj.Type;
        }

        public override bool Equals(object obj)
        {
            return obj is JoinType joinType && Equals(joinType);
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
}
